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

    bool spiralActive;
    float spiralTimer;
    float spiralDuration;
    float spiralDirection;

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
    }

    void StartSpiral()
    {
        spiralActive = true;
        spiralTimer = 0f;
        spiralDuration = Random.Range(4f, 7f);
        spiralDirection = Random.value > 0.5f ? 1f : -1f;
    }

    void UpdateSpiral()
    {
        if (!spiralActive) return;

        spiralTimer += Time.deltaTime;

        if (spiralTimer > spiralDuration)
        {
            spiralActive = false;
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

            // Ease in and ease out spiral power
            float envelope = Mathf.Sin(progress * Mathf.PI);

            float spiralStrength =
                Mathf.Lerp(0.8f, 1.6f, progress);

            float spiralCore =
                spiralDirection *
                spiralStrength *
                arcAmplitude *
                globalAmplitudeMultiplier;

            float wobble =
                Mathf.Sin(t * 6f) * 0.15f;

            // BLEND instead of hard override
            targetCurvature =
                Mathf.Lerp(targetCurvature,
                           spiralCore + wobble,
                           envelope);
        }


        if (flowCorridorActive)
        {
            float lockedWave =
                Mathf.Sin(t * arcFrequency * 1.2f);

            targetCurvature =
                lockedWave *
                arcAmplitude *
                globalAmplitudeMultiplier;

            smoothWidth =
                Mathf.Lerp(smoothWidth, 1f, Time.deltaTime * 2f);
        }

        if (directionMemoryTimer > 6f && Mathf.Abs(curvature) < 0.3f)
        {
            directionMemoryTimer = 0f;
            directionBias *= -1f;
        }

        // TRUE SPRING — no dampening of amplitude
        float springForce = (targetCurvature - curvature) * 12f;
        curvatureVelocity += springForce * Time.deltaTime;
        curvatureVelocity *= 0.97f;

        curvature += curvatureVelocity * Time.deltaTime;
        curvature = Mathf.Clamp(curvature, -1f, 1f);

        float compressionFactor = compressionActive ? 0.65f : 1f;

        float dynamicWidth =
            (0.9f - Mathf.Abs(curvature) * 0.35f) *
            compressionFactor;

        smoothWidth =
            Mathf.Lerp(smoothWidth, dynamicWidth, Time.deltaTime * 2f);

        float dynamicBank =
            0.7f + curvature * 0.5f;

        smoothBank =
            Mathf.Lerp(smoothBank, dynamicBank, Time.deltaTime * 3f);

        width = Mathf.Clamp01(smoothWidth);
        banking = Mathf.Clamp01(smoothBank);

        elevationEnergy =
            0.5f + Mathf.Sin(t * 0.2f) * 0.25f;

        tempest = Mathf.Clamp01(Mathf.Abs(curvature));
        serenity = 1f - tempest;
    }
}
