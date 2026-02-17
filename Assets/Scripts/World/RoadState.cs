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

    // 🌀 Spiral Layer
    bool spiralActive;
    float spiralTimer;
    float spiralDuration;
    float spiralDirection;

    // 🌊 Flow Corridor
    bool flowCorridorActive;
    float flowTimer;
    float flowDuration;

    // 🔻 Compression
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

    // 🎵 Echo system
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

    // =============================
    // 🌀 SPIRAL
    // =============================
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
            StartFlowCorridor(); // Always reward
        }
    }

    // =============================
    // 🌊 FLOW CORRIDOR
    // =============================
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

    // =============================
    // 🔻 COMPRESSION
    // =============================
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

    // =============================
    // CORE GEOMETRY SYSTEM
    // =============================
    void UpdateGeometry()
    {
        float t = Time.time;

        // 🌊 1. CURVATURE BREATHING
        float macroBreath = Mathf.Sin(t * 0.05f) * 0.5f + 0.5f;

        float amplitude =
            arcAmplitude *
            macroBreath *
            globalAmplitudeMultiplier;

        // 🎵 2. HARMONIC RESONANCE CURVE
        float baseWave = Mathf.Sin(t * arcFrequency);
        float harmonic = 0.35f * Mathf.Sin(t * arcFrequency * 3f);

        float combinedWave = baseWave + harmonic;

        // 🔄 Elastic shaping
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

        // 🌀 3. SPIRAL WOBBLE DISTORTION
        if (spiralActive)
        {
            float progress = spiralTimer / spiralDuration;
            float spiralStrength = Mathf.Lerp(0.8f, 1.6f, progress);

            float wobble =
                Mathf.Sin(t * 6f) * 0.15f; // subtle instability

            targetCurvature =
                spiralDirection *
                spiralStrength *
                arcAmplitude *
                globalAmplitudeMultiplier
                + wobble;
        }

        // 🌊 4. CORRIDOR SYMMETRY LOCK
        if (flowCorridorActive)
        {
            float lockedWave =
                Mathf.Sin(t * arcFrequency * 1.2f);

            targetCurvature =
                lockedWave *
                arcAmplitude *
                globalAmplitudeMultiplier;

            smoothWidth =
                Mathf.Lerp(smoothWidth,
                           1f,
                           Time.deltaTime * 2f);
        }

        // 🧠 5. MOMENTUM MEMORY
        if (directionMemoryTimer > 6f)
        {
            directionMemoryTimer = 0f;

            if (Mathf.Sign(targetCurvature) == Mathf.Sign(curvature))
                directionBias *= -1f;
        }

        // 🔁 6. CURVATURE ECHO TRAILS (overshoot effect)
        float echoForce = (targetCurvature - curvature) * 4f;
        curvatureVelocity += echoForce * Time.deltaTime;
        curvatureVelocity *= 0.92f;

        curvature += curvatureVelocity * Time.deltaTime;
        curvature = Mathf.Lerp(curvature, targetCurvature, Time.deltaTime * 3f);
        curvature = Mathf.Clamp(curvature, -1f, 1f);

        // Width
        float compressionFactor = compressionActive ? 0.65f : 1f;

        float dynamicWidth =
            (0.9f - Mathf.Abs(curvature) * 0.35f) *
            compressionFactor;

        smoothWidth =
            Mathf.Lerp(smoothWidth,
                       dynamicWidth,
                       Time.deltaTime * 2f);

        // Banking
        float dynamicBank =
            0.7f + curvature * 0.5f;

        smoothBank =
            Mathf.Lerp(smoothBank,
                       dynamicBank,
                       Time.deltaTime * 3f);

        width = Mathf.Clamp01(smoothWidth);
        banking = Mathf.Clamp01(smoothBank);

        elevationEnergy =
            0.5f + Mathf.Sin(t * 0.2f) * 0.25f;

        tempest = Mathf.Clamp01(Mathf.Abs(curvature));
        serenity = 1f - tempest;
    }
}
