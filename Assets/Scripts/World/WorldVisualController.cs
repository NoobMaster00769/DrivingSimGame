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

    float smoothTempest;
    float smoothFlow;
    float skyRotation;

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

        UpdateWaterMotion();   // PERFECT WAVES
        UpdateSkybox();
    }

    void UpdateWaterMotion()
    {
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
}
