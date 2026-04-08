using UnityEngine;

public class RoadState : MonoBehaviour
{
    public PlayerDriveMetrics player;

    [Header("World Personality (Used By Visual Systems)")]
    [Range(0f, 1f)] public float serenity;
    [Range(0f, 1f)] public float tempest;

    [Header("Outputs")]
    [Range(-1f, 1f)] public float curvature;
    [Range(0f, 1f)] public float width;
    [Range(0f, 1f)] public float banking;
    [Range(0f, 1f)] public float elevationEnergy;

    [Header("Global Control")]
    [Range(0f, 3f)] public float globalAmplitudeMultiplier = 1.5f;

    [HideInInspector] public float arcAmplitude = 0.8f;
    [HideInInspector] public float arcFrequency = 1f;
    [HideInInspector] public float rhythmIntensity = 1f;


    [Header("Spiral Safety")]
    [Tooltip("Max degrees of accumulated road yaw a spiral may contribute before it is forced to end. 120–140 keeps it tight without looking weird.")]
    public float spiralYawCap = 60f;

    public bool SpiralOverBudget => spiralActive && spiralAccumulatedYaw > spiralYawCap * 0.75f;

    bool spiralActive;
    float spiralTimer;
    float spiralDuration;
    float spiralDirection;
    float spiralAccumulatedYaw;

    bool flowCorridorActive;
    float flowTimer;
    float flowDuration;

    bool compressionActive;
    float compressionTimer;
    float compressionDuration;

    float smoothWidth;
    float smoothBank;

    float rhythmTimer;
    float rhythmDuration;
    int rhythmPhase;

    float directionBias = 1f;
    float directionMemoryTimer;

    float curvatureVelocity;



    [Header("Width Events")]
    [Tooltip("0–1 chance per rhythm cycle that a narrow section triggers.")]
    [Range(0f, 1f)] public float narrowChance = 0.45f;
    [Tooltip("How far the road narrows. 1 = fully narrow (inspector width min), 0 = no change.")]
    [Range(0f, 1f)] public float narrowStrength = 0.72f;  
    [Tooltip("Seconds the road stays at peak narrowness.")]
    public float narrowHoldDuration = 3.5f;
    [Tooltip("Seconds to widen out again after a narrow event (also used as intro ramp).")]
    public float narrowRampDuration = 2.0f;

    [Tooltip("0–1 chance a wide-open section follows a narrow event.")]
    [Range(0f, 1f)] public float wideAfterNarrow = 0.55f;
    [Tooltip("How wide the road expands. 0 = no change, 1 = fully open.")]
    [Range(0f, 1f)] public float wideStrength = 0.25f;  
    public float wideHoldDuration = 4.0f;
    public float wideRampDuration = 2.5f;

    [Tooltip("Amplitude of the always-on slow breathing pulse (subtle).")]
    [Range(0f, 0.12f)] public float widthPulseAmplitude = 0.06f;
    [Tooltip("Speed of the width breathing cycle in seconds (larger = slower).")]
    public float widthPulsePeriod = 11f;


    enum WidthEventType { None, NarrowIn, NarrowHold, NarrowOut, WideIn, WideHold, WideOut }
    WidthEventType widthEvent = WidthEventType.None;
    float widthEventTimer;
    float widthEventTarget = 0.5f;   
    float widthEventOrigin = 0.5f;   



    void Start()
    {
        PickNewRhythm();
    }

    void Update()
    {
        if (!player) return;

        rhythmTimer += Time.deltaTime;
        directionMemoryTimer += Time.deltaTime;

        if (rhythmTimer > rhythmDuration)
            PickNewRhythm();

        UpdateSpiral();
        UpdateFlowCorridor();
        UpdateCompression();
        UpdateWidthEvent();   
        UpdateGeometry();
    }



    void PickNewRhythm()
    {
        rhythmTimer = 0f;
        rhythmDuration = Random.Range(8f, 18f);
        rhythmPhase = Random.Range(0, 4);

        directionBias = Random.value > 0.5f ? 1.1f : -1.1f;

        if (!spiralActive && !flowCorridorActive && Random.value < 0.35f)
            StartSpiral();

        if (Random.value < 0.4f)
            StartCompression();


        if (widthEvent == WidthEventType.None && Random.value < narrowChance)
            StartNarrow();

    }

    void StartSpiral()
    {
        spiralActive = true;
        spiralTimer = 0f;
        spiralDuration = Random.Range(4f, 7f);
        spiralDirection = Random.value > 0.5f ? 1f : -1f;
        spiralAccumulatedYaw = 0f;
    }

    void UpdateSpiral()
    {
        if (!spiralActive) return;

        spiralTimer += Time.deltaTime;

        float contribution = curvature * spiralDirection;
        if (contribution > 0f)
            spiralAccumulatedYaw += contribution * 18f * Time.deltaTime;

        if (spiralTimer > spiralDuration || spiralAccumulatedYaw >= spiralYawCap)
        {
            spiralActive = false;
            spiralAccumulatedYaw = 0f;
            StartFlowCorridor();
        }
    }

    void StartFlowCorridor()
    {
        flowCorridorActive = true;
        flowTimer = 0f;
        flowDuration = Random.Range(3f, 6f);
    }

    void UpdateFlowCorridor()
    {
        if (!flowCorridorActive) return;

        flowTimer += Time.deltaTime;

        if (flowTimer > flowDuration)
            flowCorridorActive = false;
    }

    void StartCompression()
    {
        compressionActive = true;
        compressionTimer = 0f;
        compressionDuration = Random.Range(3f, 6f);
    }

    void UpdateCompression()
    {
        if (!compressionActive) return;

        compressionTimer += Time.deltaTime;

        if (compressionTimer > compressionDuration)
            compressionActive = false;
    }



    void StartNarrow()
    {
        widthEvent = WidthEventType.NarrowIn;
        widthEventTimer = 0f;
        widthEventOrigin = widthEventTarget;        
    }

    void StartWide()
    {
        widthEvent = WidthEventType.WideIn;
        widthEventTimer = 0f;
        widthEventOrigin = widthEventTarget;
    }

    void UpdateWidthEvent()
    {
        widthEventTimer += Time.deltaTime;

        switch (widthEvent)
        {

            case WidthEventType.NarrowIn:
                {
                    float target = 1f - narrowStrength;   
                    float frac = Mathf.Clamp01(widthEventTimer / narrowRampDuration);
                    widthEventTarget = Mathf.Lerp(widthEventOrigin, target, Mathf.SmoothStep(0f, 1f, frac));
                    if (frac >= 1f)
                    {
                        widthEvent = WidthEventType.NarrowHold;
                        widthEventTimer = 0f;
                        widthEventOrigin = widthEventTarget;
                    }
                    break;
                }


            case WidthEventType.NarrowHold:
                if (widthEventTimer >= narrowHoldDuration)
                {
                    widthEvent = WidthEventType.NarrowOut;
                    widthEventTimer = 0f;
                    widthEventOrigin = widthEventTarget;
                }
                break;


            case WidthEventType.NarrowOut:
                {
                    float target = 0.5f;                  
                    float frac = Mathf.Clamp01(widthEventTimer / narrowRampDuration);
                    widthEventTarget = Mathf.Lerp(widthEventOrigin, target, Mathf.SmoothStep(0f, 1f, frac));
                    if (frac >= 1f)
                    {
                        if (Random.value < wideAfterNarrow)
                            StartWide();
                        else
                        {
                            widthEvent = WidthEventType.None;
                            widthEventTimer = 0f;
                        }
                    }
                    break;
                }


            case WidthEventType.WideIn:
                {
                    float target = 0.5f + wideStrength * 0.5f;   
                    float frac = Mathf.Clamp01(widthEventTimer / wideRampDuration);
                    widthEventTarget = Mathf.Lerp(widthEventOrigin, target, Mathf.SmoothStep(0f, 1f, frac));
                    if (frac >= 1f)
                    {
                        widthEvent = WidthEventType.WideHold;
                        widthEventTimer = 0f;
                        widthEventOrigin = widthEventTarget;
                    }
                    break;
                }


            case WidthEventType.WideHold:
                if (widthEventTimer >= wideHoldDuration)
                {
                    widthEvent = WidthEventType.WideOut;
                    widthEventTimer = 0f;
                    widthEventOrigin = widthEventTarget;
                }
                break;


            case WidthEventType.WideOut:
                {
                    float target = 0.5f;
                    float frac = Mathf.Clamp01(widthEventTimer / wideRampDuration);
                    widthEventTarget = Mathf.Lerp(widthEventOrigin, target, Mathf.SmoothStep(0f, 1f, frac));
                    if (frac >= 1f)
                    {
                        widthEvent = WidthEventType.None;
                        widthEventTimer = 0f;
                    }
                    break;
                }


            case WidthEventType.None:
                widthEventTarget = 0.5f + Mathf.Sin(Time.time * (Mathf.PI * 2f / widthPulsePeriod))
                                        * widthPulseAmplitude;
                break;
        }
    }


    void UpdateGeometry()
    {
        float t = Time.time;

        float macroBreath = Mathf.Sin(t * 0.05f) * 0.5f + 0.5f;

        float amplitude =
            arcAmplitude *
            macroBreath *
            globalAmplitudeMultiplier;

        float baseWave = Mathf.Sin(t * arcFrequency);
        float harmonic = 0.35f * Mathf.Sin(t * arcFrequency * 3f);

        float combinedWave = baseWave + harmonic;

        float elasticWave =
            Mathf.Sign(combinedWave) *
            Mathf.Pow(Mathf.Abs(combinedWave), 1.35f);

        float rhythmStrength = 1f;

        switch (rhythmPhase)
        {
            case 0: rhythmStrength = 0.5f; break;
            case 1: rhythmStrength = 0.9f; break;
            case 2: rhythmStrength = 1.3f; break;
            case 3: rhythmStrength = 0.7f; break;
        }

        float targetCurvature =
            elasticWave *
            amplitude *
            rhythmStrength *
            directionBias *
            rhythmIntensity;

        if (spiralActive)
        {
            float progress = spiralTimer / spiralDuration;

            float budgetFraction = Mathf.Clamp01(spiralAccumulatedYaw / spiralYawCap);
            float budgetFade = 1f - Mathf.SmoothStep(0.7f, 1f, budgetFraction);

            float envelope = Mathf.Sin(progress * Mathf.PI) * budgetFade;

            float spiralStrength = Mathf.Lerp(0.8f, 1.6f, progress);

            float spiralCore =
                spiralDirection *
                spiralStrength *
                arcAmplitude *
                globalAmplitudeMultiplier;

            float wobble = Mathf.Sin(t * 6f) * 0.15f;

            targetCurvature =
                Mathf.Lerp(targetCurvature, spiralCore + wobble, envelope);
        }

        if (flowCorridorActive)
        {
            float lockedWave = Mathf.Sin(t * arcFrequency * 1.2f);

            targetCurvature =
                lockedWave *
                arcAmplitude *
                globalAmplitudeMultiplier;

            smoothWidth = Mathf.Lerp(smoothWidth, 1f, Time.deltaTime * 2f);
        }

        if (directionMemoryTimer > 6f && Mathf.Abs(curvature) < 0.3f)
        {
            directionMemoryTimer = 0f;
            directionBias *= -1f;
        }

        float springForce = (targetCurvature - curvature) * 12f;
        curvatureVelocity += springForce * Time.deltaTime;
        curvatureVelocity *= 0.97f;

        curvature += curvatureVelocity * Time.deltaTime;
        curvature = Mathf.Clamp(curvature, -1f, 1f);

        float compressionFactor = compressionActive ? 0.65f : 1f;

        float dynamicWidth =
            (0.9f - Mathf.Abs(curvature) * 0.35f) *
            compressionFactor;

        smoothWidth = Mathf.Lerp(smoothWidth, dynamicWidth, Time.deltaTime * 2f);

        float dynamicBank = 0.7f + curvature * 0.5f;

        smoothBank = Mathf.Lerp(smoothBank, dynamicBank, Time.deltaTime * 3f);


        smoothWidth = Mathf.Lerp(smoothWidth, widthEventTarget, Time.deltaTime * 1.5f);


        width = Mathf.Clamp01(smoothWidth);
        banking = Mathf.Clamp01(smoothBank);

        elevationEnergy = 0.5f + Mathf.Sin(t * 0.2f) * 0.25f;

        tempest = Mathf.Clamp01(Mathf.Abs(curvature));
        serenity = 1f - tempest;
    }
}