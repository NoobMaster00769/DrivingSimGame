using UnityEngine;

public class WorldVisualController : MonoBehaviour
{
    public RoadState roadState;
    public PlayerDriveMetrics metrics;

    [Header("Materials")]
    public Material roadMaterial;
    public Material skyMaterial;
    public Material waterMaterial;

    [Header("Colors")]
    public Color serenityColor = new Color(0.05f, 0.12f, 0.2f);
    public Color tempestColor = new Color(0.02f, 0.03f, 0.15f);
    public Color flowTint = new Color(0.1f, 0.25f, 0.3f);

    float smoothTempest;
    float smoothSerenity;
    float smoothFlow;

    void Update()
    {
        if (!roadState || !metrics) return;

        smoothTempest = Mathf.Lerp(
            smoothTempest,
            roadState.tempest,
            Time.deltaTime * 0.6f
        );

        smoothSerenity = Mathf.Lerp(
            smoothSerenity,
            roadState.serenity,
            Time.deltaTime * 0.6f
        );

        smoothFlow = Mathf.Lerp(
            smoothFlow,
            metrics.flow,
            Time.deltaTime * 0.6f
        );

        UpdateWater();
        UpdateRoad();
        UpdateSky();
        UpdateFog();
    }

    void UpdateWater()
    {
        if (!waterMaterial) return;

        Color moodColor =
            Color.Lerp(serenityColor, tempestColor, smoothTempest);

        moodColor =
            Color.Lerp(moodColor, flowTint, smoothFlow * 0.4f);

        waterMaterial.SetColor("_ShallowColor", moodColor);
        waterMaterial.SetFloat("_WaveScale",
            Mathf.Lerp(0.02f, 0.08f, smoothTempest));

        waterMaterial.SetFloat("_WaveSpeed",
            Mathf.Lerp(0.2f, 0.6f, smoothFlow));
    }

    void UpdateRoad()
    {
        if (!roadMaterial) return;

        roadMaterial.SetFloat("_Intensity", metrics.intensity);

        float glow =
            0.6f +
            smoothFlow * 0.8f;

        Color roadColor =
            Color.Lerp(serenityColor, flowTint, smoothFlow);

        roadMaterial.SetColor(
            "_EmissionColor",
            roadColor * glow
        );
    }

    void UpdateSky()
    {
        if (!skyMaterial) return;

        skyMaterial.SetFloat("_Tempest", smoothTempest);
        skyMaterial.SetFloat("_StarBrightness",
            Mathf.Lerp(0.3f, 1.5f, smoothTempest));
    }

    void UpdateFog()
    {
        Color fogColor =
            Color.Lerp(serenityColor, tempestColor, smoothTempest);

        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity =
            Mathf.Lerp(0.004f, 0.012f, smoothTempest);
    }
}
