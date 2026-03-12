using UnityEngine;

public class WorldVisualController : MonoBehaviour
{
    public RoadState roadState;
    public WorldEventDirector director;   // 🔥 link this
    public Material waterMaterial;

    [Header("Wave Motion")]
    public float minWaveScale = 0.01f;
    public float maxWaveScale = 0.018f;
    public float minWaveSpeed = 0.03f;
    public float maxWaveSpeed = 0.06f;

    [Header("Skybox Rotation")]
    public float baseSkyRotationSpeed = 0.2f;

    [Header("Arc Sky Gradients (Match Arc Index Order)")]
    public Gradient[] arcGradients;   // MUST match arc indices (0–6)

    public float arcDuration = 480f;

    float skyRotation;
    float arcTimer;

    void Update()
    {
        if (!roadState || !waterMaterial || !director)
            return;

        arcTimer += Time.deltaTime;

        if (arcTimer > arcDuration)
            arcTimer = 0f;

        UpdateWaterMotion();   // untouched
        UpdateSkybox();
        UpdateSkyColor();
    }

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
        if (arcGradients.Length == 0) return;

        int arcIndex = director.CurrentArcIndex;

        if (arcIndex >= arcGradients.Length)
            arcIndex = 0;

        float t = arcTimer / arcDuration;

        Color c = arcGradients[arcIndex].Evaluate(t);

        RenderSettings.skybox.SetColor("_Tint", c);

        // ⭐ Force realtime environment update
        DynamicGI.UpdateEnvironment();
    }
}
