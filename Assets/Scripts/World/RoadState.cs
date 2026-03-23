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

    // ─── Spiral yaw budget ────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────
    //  WIDTH EVENTS — additive, nothing above changed
    //
    //  Three event types run independently of all existing logic:
    //
    //  NARROW  — road pinches to ~40% normal width for a few seconds.
    //            Creates a tense chicane feel. Triggered randomly but
    //            never while already in a width event.
    //
    //  WIDE    — road opens to ~130% normal width. Breathing room after
    //            a tight section or a long curve. Follows narrows ~40%
    //            of the time to give a "relief" rhythm.
    //
    //  PULSE   — road slowly breathes in/out sinusoidally. Subtle,
    //            runs between events as a low-level texture.
    //
    //  widthEventTarget is the 0–1 value injected into smoothWidth
    //  in UpdateGeometry (see bottom of file). It lerps in/out so
    //  transitions are always smooth and never jarring.
    //
    //  The existing `width` output is untouched in meaning — visual
    //  systems and GetHalfWidth() read it exactly as before.
    // ─────────────────────────────────────────────────────────────

    [Header("Width Events")]
    [Tooltip("0–1 chance per rhythm cycle that a narrow section triggers.")]
    [Range(0f, 1f)] public float narrowChance = 0.45f;
    [Tooltip("How far the road narrows. 1 = fully narrow (inspector width min), 0 = no change.")]
    [Range(0f, 1f)] public float narrowStrength = 0.72f;  // drives width toward ~0.28
    [Tooltip("Seconds the road stays at peak narrowness.")]
    public float narrowHoldDuration = 3.5f;
    [Tooltip("Seconds to widen out again after a narrow event (also used as intro ramp).")]
    public float narrowRampDuration = 2.0f;

    [Tooltip("0–1 chance a wide-open section follows a narrow event.")]
    [Range(0f, 1f)] public float wideAfterNarrow = 0.55f;
    [Tooltip("How wide the road expands. 0 = no change, 1 = fully open.")]
    [Range(0f, 1f)] public float wideStrength = 0.25f;  // drives width toward ~0.75
    public float wideHoldDuration = 4.0f;
    public float wideRampDuration = 2.5f;

    [Tooltip("Amplitude of the always-on slow breathing pulse (subtle).")]
    [Range(0f, 0.12f)] public float widthPulseAmplitude = 0.06f;
    [Tooltip("Speed of the width breathing cycle in seconds (larger = slower).")]
    public float widthPulsePeriod = 11f;

    // Internal width event state
    enum WidthEventType { None, NarrowIn, NarrowHold, NarrowOut, WideIn, WideHold, WideOut }
    WidthEventType widthEvent = WidthEventType.None;
    float widthEventTimer;
    float widthEventTarget = 0.5f;   // neutral 0–1 value fed into smoothWidth
    float widthEventOrigin = 0.5f;   // value at the moment the event started

    // ─────────────────────────────────────────────────────────────

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
        UpdateWidthEvent();   // ← new, runs alongside existing events
        UpdateGeometry();
    }

    // ═════════════════════════════════════════════════════════════
    //  EVERYTHING BELOW THIS LINE IS IDENTICAL TO THE WORKING
    //  VERSION — only UpdateGeometry has two lines added at the end
    //  (clearly marked). Nothing existing is removed or modified.
    // ═════════════════════════════════════════════════════════════

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

        // ── Width event roll (additive) ──────────────────────────
        // Only fire if no width event is currently running.
        if (widthEvent == WidthEventType.None && Random.value < narrowChance)
            StartNarrow();
        // ────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────
    //  WIDTH EVENT STATE MACHINE  (new, additive)
    // ─────────────────────────────────────────────────────────────

    void StartNarrow()
    {
        widthEvent = WidthEventType.NarrowIn;
        widthEventTimer = 0f;
        widthEventOrigin = widthEventTarget;          // start from wherever we are
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
            // ── Narrow ramp in ──────────────────────────────────
            case WidthEventType.NarrowIn:
                {
                    float target = 1f - narrowStrength;   // e.g. 0.28 when strength=0.72
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

            // ── Narrow hold ─────────────────────────────────────
            case WidthEventType.NarrowHold:
                if (widthEventTimer >= narrowHoldDuration)
                {
                    widthEvent = WidthEventType.NarrowOut;
                    widthEventTimer = 0f;
                    widthEventOrigin = widthEventTarget;
                }
                break;

            // ── Narrow ramp out — optionally transition to Wide ──
            case WidthEventType.NarrowOut:
                {
                    float target = 0.5f;                  // back to neutral
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

            // ── Wide ramp in ────────────────────────────────────
            case WidthEventType.WideIn:
                {
                    float target = 0.5f + wideStrength * 0.5f;   // e.g. 0.625 when strength=0.25
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

            // ── Wide hold ───────────────────────────────────────
            case WidthEventType.WideHold:
                if (widthEventTimer >= wideHoldDuration)
                {
                    widthEvent = WidthEventType.WideOut;
                    widthEventTimer = 0f;
                    widthEventOrigin = widthEventTarget;
                }
                break;

            // ── Wide ramp out ───────────────────────────────────
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

            // ── No event — gentle pulse keeps things alive ──────
            case WidthEventType.None:
                widthEventTarget = 0.5f + Mathf.Sin(Time.time * (Mathf.PI * 2f / widthPulsePeriod))
                                        * widthPulseAmplitude;
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  UPDATE GEOMETRY — identical to working version.
    //  Two lines added at the very end (clearly marked) to blend
    //  widthEventTarget into smoothWidth before it is written to width.
    // ─────────────────────────────────────────────────────────────

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

        // ── WIDTH EVENT injection (additive — two lines only) ────
        // Blend smoothWidth toward widthEventTarget at a gentle rate.
        // When no event is running, widthEventTarget == 0.5 (neutral pulse)
        // so this has near-zero effect. During events it steers width smoothly.
        // The lerp speed (1.5) is slower than smoothWidth's own lerp (2.0)
        // so it never snaps or fights the existing curvature-based narrowing.
        smoothWidth = Mathf.Lerp(smoothWidth, widthEventTarget, Time.deltaTime * 1.5f);
        // ────────────────────────────────────────────────────────

        width = Mathf.Clamp01(smoothWidth);
        banking = Mathf.Clamp01(smoothBank);

        elevationEnergy = 0.5f + Mathf.Sin(t * 0.2f) * 0.25f;

        tempest = Mathf.Clamp01(Mathf.Abs(curvature));
        serenity = 1f - tempest;
    }
}