using UnityEngine;

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

    [Header("Skybox Runtime Material")]
    public Material skyboxRuntime; // MUST use Cubemap Blend shader

    void Start()
    {
        // Force runtime skybox material
        RenderSettings.skybox = skyboxRuntime;
    }

    void Update()
    {
        ApplyTimeOfDay(time);
    }

    void ApplyTimeOfDay(float t)
    {
        // -------- SKYBOX (ONLY Day -> Night) --------
        skyboxRuntime.SetFloat("_CubemapTransition", t);
        skyboxRuntime.SetFloat("_CubemapExposure", Mathf.Lerp(day.skyExposure, night.skyExposure, t));

        // -------- LIGHTING (Day -> Blend -> Night) --------
        if (t <= 0.5f)
        {
            float k = t / 0.5f;
            ApplyLighting(day, blend, k);
        }
        else
        {
            float k = (t - 0.5f) / 0.5f;
            ApplyLighting(blend, night, k);
        }
    }

    void ApplyLighting(TimeOfDay a, TimeOfDay b, float t)
    {
        // Sun
        sun.color = Color.Lerp(a.sunColor, b.sunColor, t);
        sun.intensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t);

        // Fog
        RenderSettings.fog = a.fogEnabled || b.fogEnabled;
        RenderSettings.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
        RenderSettings.fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t);

        // Ambient
        RenderSettings.ambientLight = Color.Lerp(a.ambientColor, b.ambientColor, t);
    }
}
