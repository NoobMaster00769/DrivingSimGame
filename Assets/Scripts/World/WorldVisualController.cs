using UnityEngine;

public class WorldVisualController : MonoBehaviour
{
    public RoadState roadState;
    public PlayerDriveMetrics metrics;

    [Header("Water Material")]
    public Material waterMaterial;

    [Header("Wave Motion")]
    public float minWaveScale = 0.01f;
    public float maxWaveScale = 0.018f;

    public float minWaveSpeed = 0.03f;
    public float maxWaveSpeed = 0.06f;

    [Header("Skybox Rotation")]
    public float baseSkyRotationSpeed = 0.2f;
    public float tempestSkyBoost = 1.5f;
    public float flowSkyBoost = 1.0f;

    [Header("Illusion Elevation")]
    public float elevationBreathSpeed = 0.2f;
    public float elevationIntensity = 15f;   // illusion strength
    public Camera mainCamera;
    public float fovBoostAmount = 3f;

    float smoothTempest;
    float smoothFlow;

    float skyRotation;
    float baseFOV;
    float elevationPhase;

    void Start()
    {
        if (mainCamera)
            baseFOV = mainCamera.fieldOfView;
    }

    void Update()
    {
        if (!roadState || !metrics || !waterMaterial)
            return;

        smoothTempest = Mathf.Lerp(
            smoothTempest,
            roadState.tempest,
            Time.deltaTime * 0.6f
        );

        smoothFlow = Mathf.Lerp(
            smoothFlow,
            metrics.flow,
            Time.deltaTime * 0.6f
        );

        UpdateWaterMotion();      // UNCHANGED
        UpdateSkybox();           // enhanced safely
        UpdateElevationIllusion();// NEW (visual only)
    }

    void UpdateWaterMotion()
    {
        // UNTOUCHED — your waves remain perfect

        float pulse = Mathf.Sin(Time.time * 0.5f) * 0.002f;

        float waveScale =
            Mathf.Lerp(minWaveScale, maxWaveScale, smoothTempest) + pulse;

        float waveSpeed =
            Mathf.Lerp(minWaveSpeed, maxWaveSpeed, smoothFlow);

        waterMaterial.SetFloat("_WaveScale", waveScale);
        waterMaterial.SetFloat("_WaveSpeed", waveSpeed);
    }

    void UpdateSkybox()
    {
        if (RenderSettings.skybox == null)
            return;

        float rotationSpeed =
            baseSkyRotationSpeed +
            smoothTempest * tempestSkyBoost +
            smoothFlow * flowSkyBoost;

        skyRotation += rotationSpeed * Time.deltaTime;

        RenderSettings.skybox.SetFloat("_Rotation", skyRotation);
    }

    void UpdateElevationIllusion()
    {
        // Fake vertical world breathing

        elevationPhase += Time.deltaTime * elevationBreathSpeed;

        float elevationWave =
            Mathf.Sin(elevationPhase) * elevationIntensity * roadState.elevationEnergy;

        // Subtle skybox vertical illusion
        if (RenderSettings.skybox.HasProperty("_Exposure"))
        {
            float exposure =
                1f + elevationWave * 0.01f;

            RenderSettings.skybox.SetFloat("_Exposure", exposure);
        }

        // Subtle FOV shift illusion
        if (mainCamera)
        {
            float targetFOV =
                baseFOV +
                elevationWave * 0.05f +
                smoothFlow * fovBoostAmount;

            mainCamera.fieldOfView =
                Mathf.Lerp(
                    mainCamera.fieldOfView,
                    targetFOV,
                    Time.deltaTime * 2f
                );
        }
    }
}
