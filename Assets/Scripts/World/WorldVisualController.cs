using UnityEngine;

public class WorldVisualController : MonoBehaviour
{
    public RoadState roadState;
    public PlayerDriveMetrics metrics;

    [Header("Materials")]
    public Material surfaceMaterial;
    public Material roadMaterial;
    public Material skyMaterial;

    [Header("Colors")]
    public Color serenityColor = new Color(0.05f, 0.12f, 0.2f);
    public Color tempestColor = new Color(0.02f, 0.03f, 0.15f);
    public Color flowTint = new Color(0.1f, 0.25f, 0.3f);

    [Header("Breathing")]
    public float breatheSpeed = 0.4f;
    public float breatheAmplitude = 0.05f;

    float breathePhase;
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

        breathePhase += Time.deltaTime * breatheSpeed;

        UpdateSurface();
        UpdateRoad();
        UpdateSky();
        UpdateFog();
    }

    // ==========================
    // DREAM SURFACE
    // ==========================
    void UpdateSurface()
    {
        if (!surfaceMaterial) return;

        float breathe =
            Mathf.Sin(breathePhase) * breatheAmplitude;

        // Blend serenity + tempest
        Color moodColor =
            Color.Lerp(serenityColor, tempestColor, smoothTempest);

        moodColor =
            Color.Lerp(moodColor, flowTint, smoothFlow * 0.5f);

        Color finalColor =
            moodColor * (1f + breathe);

        surfaceMaterial.SetColor("_BaseColor", finalColor);

        float emission =
            0.5f +
            smoothTempest * 0.8f +
            smoothFlow * 0.6f;

        surfaceMaterial.SetColor(
            "_EmissionColor",
            finalColor * emission
        );

        // Mood driver for shader ripple
        surfaceMaterial.SetFloat("_Mood", smoothTempest);
    }

    // ==========================
    // ROAD MATERIAL
    // ==========================
    void UpdateRoad()
    {
        if (!roadMaterial) return;

        // Instead of agitation, use intensity from metrics
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

    // ==========================
    // SKY MATERIAL
    // ==========================
    void UpdateSky()
    {
        if (!skyMaterial) return;

        skyMaterial.SetFloat("_Tempest", smoothTempest);

        float starIntensity =
            Mathf.Lerp(0.3f, 1.5f, smoothTempest);

        skyMaterial.SetFloat("_StarBrightness", starIntensity);

        // subtle rotation driver
        skyMaterial.SetFloat("_Flow", smoothFlow);
    }

    // ==========================
    // FOG
    // ==========================
    void UpdateFog()
    {
        Color fogColor =
            Color.Lerp(serenityColor, tempestColor, smoothTempest);

        RenderSettings.fogColor = fogColor;

        RenderSettings.fogDensity =
            Mathf.Lerp(0.004f, 0.012f, smoothTempest);
    }
}