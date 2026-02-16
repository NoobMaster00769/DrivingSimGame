using UnityEngine;

public class RoadState : MonoBehaviour
{
    public PlayerDriveMetrics player;

    [Header("World Personality")]
    [Range(0, 1)] public float serenity;
    [Range(0, 1)] public float tempest;

    [Header("Outputs")]
    [Range(-1f, 1f)] public float curvature;
    [Range(0f, 1f)] public float width;
    [Range(0f, 1f)] public float banking;
    [Range(0f, 1f)] public float elevationEnergy;

    [Header("CURVATURE POWER CONTROL")]
    [Range(0f, 3f)] public float globalAmplitudeMultiplier = 1.5f;
    [Range(0f, 3f)] public float driftStrength = 1.2f;
    [Range(0f, 3f)] public float waveStrength = 1.5f;

    // Controlled by WorldEventDirector
    [HideInInspector] public float arcAmplitude = 0.6f;
    [HideInInspector] public float arcFrequency = 0.8f;
    [HideInInspector] public float arcWidthTarget = 0.8f;
    [HideInInspector] public float arcBankTarget = 0.7f;

    float smoothWidth;
    float smoothBank;

    void Update()
    {
        if (!player) return;

        float t = Time.time;

        float baseWave = Mathf.Sin(t * arcFrequency) * waveStrength;

        float drift = (Mathf.PerlinNoise(t * 0.15f, 0f) - 0.5f) * 0.6f * driftStrength;

        float targetCurvature =
            (baseWave + drift) *
            arcAmplitude *
            globalAmplitudeMultiplier;

        curvature = Mathf.Lerp(curvature, targetCurvature, Time.deltaTime * 2f);

        float dynamicWidth = arcWidthTarget - Mathf.Abs(curvature) * 0.25f;
        smoothWidth = Mathf.Lerp(smoothWidth, dynamicWidth, Time.deltaTime * 1.5f);

        float dynamicBank = arcBankTarget + curvature * 0.4f;
        smoothBank = Mathf.Lerp(smoothBank, dynamicBank, Time.deltaTime * 2f);

        width = Mathf.Clamp01(smoothWidth);
        banking = Mathf.Clamp01(smoothBank);

        elevationEnergy = 0.5f + Mathf.Sin(t * 0.2f) * 0.25f;

        serenity = 1f - Mathf.Abs(curvature);
        tempest = Mathf.Abs(curvature);
    }
}
