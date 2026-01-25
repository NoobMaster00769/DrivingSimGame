using UnityEngine;
using UnityEngine.Rendering;

public class TimeOfDayController : MonoBehaviour
{
    public Light sun;
    public TimeOfDay day;
    public TimeOfDay blend;
    public TimeOfDay night;

    [Range(0f, 1f)]
    public float time;

    public Material skyboxRuntime;

    void Start()
    {
        RenderSettings.skybox = skyboxRuntime;

        // 🔒 Lock ambient
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.gray;
        RenderSettings.ambientIntensity = 1f;
    }

    void Update()
    {
        ApplyTimeOfDay(time);
    }

    void ApplyTimeOfDay(float t)
    {
        skyboxRuntime.SetFloat("_CubemapTransition", t);
        skyboxRuntime.SetFloat(
            "_CubemapExposure",
            Mathf.Lerp(day.skyExposure, night.skyExposure, t)
        );

        if (t <= 0.5f)
            ApplyLighting(day, blend, t / 0.5f);
        else
            ApplyLighting(blend, night, (t - 0.5f) / 0.5f);
    }

    void ApplyLighting(TimeOfDay a, TimeOfDay b, float t)
    {
        sun.color = Color.Lerp(a.sunColor, b.sunColor, t);
        sun.intensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t);

        // ✅ Fog only, NOT ambient
        RenderSettings.fog = a.fogEnabled || b.fogEnabled;
        RenderSettings.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
        RenderSettings.fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t);
    }
}
