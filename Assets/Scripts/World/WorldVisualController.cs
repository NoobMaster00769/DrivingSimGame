using UnityEngine;

public class WorldVisualController : MonoBehaviour
{
    public RoadState roadState;
    public Material waterMaterial;

    [Header("Wave Motion")]
    public float minWaveScale = 0.01f;
    public float maxWaveScale = 0.018f;
    public float minWaveSpeed = 0.03f;
    public float maxWaveSpeed = 0.06f;

    [Header("Skybox Rotation")]
    public float baseSkyRotationSpeed = 0.2f;

    [Header("Sky Color Shift")]
    public Gradient skyGradient;
    public float arcDuration = 600f;

    float skyRotation;
    float arcTimer;

    void Update()
    {
        if (!roadState || !waterMaterial)
            return;

        arcTimer += Time.deltaTime;

        UpdateWaterMotion();   // UNTOUCHED
        UpdateSkybox();
        UpdateSkyColor();
    }

    // -------------------------
    // DO NOT TOUCH — preserved
    // -------------------------
    void UpdateWaterMotion()
    {
        float pulse = Mathf.Sin(Time.time * 0.5f) * 0.002f;

        float waveScale =
            Mathf.Lerp(minWaveScale, maxWaveScale, roadState.tempest) + pulse;

        float waveSpeed =
            Mathf.Lerp(minWaveSpeed, maxWaveSpeed, 0.5f);

        waterMaterial.SetFloat("_WaveScale", waveScale);
        waterMaterial.SetFloat("_WaveSpeed", waveSpeed);
    }

    void UpdateSkybox()
    {
        if (RenderSettings.skybox == null)
            return;

        skyRotation += baseSkyRotationSpeed * Time.deltaTime;
        RenderSettings.skybox.SetFloat("_Rotation", skyRotation);
    }

    void UpdateSkyColor()
    {
        if (RenderSettings.skybox == null) return;

        float t = (arcTimer % arcDuration) / arcDuration;

        if (skyGradient != null)
        {
            Color c = skyGradient.Evaluate(t);
            RenderSettings.skybox.SetColor("_Tint", c);
        }
    }
}
