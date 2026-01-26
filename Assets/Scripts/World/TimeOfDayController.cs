using UnityEngine;
using UnityEngine.Rendering;

public class TimeOfDayController : MonoBehaviour
{
    [Header("References")]
    public Light sun;

    [Header("Presets")]
    public TimeOfDay day;
    public TimeOfDay blend;
    public TimeOfDay night;

    [Header("Runtime")]
    [Range(0f, 1f)]
    public float time;

    [Header("Skybox Runtime")]
    public Material skyboxRuntime;

    [Header("Ambient Lock")]
    public Color fixedAmbientColor = new Color(0.65f, 0.65f, 0.65f);
    public float ambientIntensity = 1f;

    void Start()
    {
        RenderSettings.skybox = skyboxRuntime;

        // 🔒 LOCK AMBIENT
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = fixedAmbientColor;
        RenderSettings.ambientIntensity = ambientIntensity;

        // 🔒 LOCK SKY-BASED INDIRECT LIGHTING (URP-safe)
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = null;
        RenderSettings.reflectionIntensity = 0f;
    }

    void Update()
    {
        ApplyTimeOfDay(time);
    }

    void ApplyTimeOfDay(float t)
    {
        // SKYBOX VISUALS ONLY
        skyboxRuntime.SetFloat("_CubemapTransition", t);
        skyboxRuntime.SetFloat(
            "_CubemapExposure",
            Mathf.Lerp(day.skyExposure, night.skyExposure, t)
        );

        // SUN: INTENSITY ONLY (no color tint)
        sun.color = Color.white;
        sun.intensity = Mathf.Lerp(day.sunIntensity, night.sunIntensity, t);
    }
}
