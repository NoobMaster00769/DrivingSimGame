using UnityEngine;

public class RuntimeLightingFix : MonoBehaviour
{
    [Header("Ambient Lock")]
    public Color ambientColor = new Color(0.65f, 0.65f, 0.65f);

    [Range(0f, 2f)]
    public float ambientIntensity = 1f;

    void Awake()
    {
        Apply();
    }

    void Apply()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.ambientIntensity = ambientIntensity;
    }
}
