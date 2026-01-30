using UnityEngine;

public class WorldMoodFog : MonoBehaviour
{
    public RoadState roadState;

    [Header("Fog Settings")]
    public float calmFogDensity = 0.02f;
    public float aggressiveFogDensity = 0.005f;

    public Color calmFogColor = new Color(0.7f, 0.8f, 0.85f);
    public Color aggressiveFogColor = new Color(0.9f, 0.85f, 0.75f);

    public float smoothSpeed = 1.5f;

    void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
    }

    void Update()
    {
        if (!roadState) return;

        float t = roadState.agitation;

        RenderSettings.fogDensity = Mathf.Lerp(
            RenderSettings.fogDensity,
            Mathf.Lerp(calmFogDensity, aggressiveFogDensity, t),
            Time.deltaTime * smoothSpeed
        );

        RenderSettings.fogColor = Color.Lerp(
            RenderSettings.fogColor,
            Color.Lerp(calmFogColor, aggressiveFogColor, t),
            Time.deltaTime * smoothSpeed
        );
    }
}
