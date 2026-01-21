using UnityEngine;

public class PaletteDrift : MonoBehaviour
{
    [System.Serializable]
    public struct Palette
    {
        public Color ambientColor;
        public Color fogColor;
        public Color lightColor;
    }

    public Palette[] palettes;
    public float minutesPerTransition = 8f;

    int currentIndex = 0;
    int nextIndex = 1;
    float t = 0f;

    Light dirLight;

    void Start()
    {
        dirLight = FindObjectOfType<Light>();
        ApplyPalette(palettes[currentIndex]);
    }

    void Update()
    {
        t += Time.deltaTime / (minutesPerTransition * 60f);

        RenderSettings.ambientLight =
            Color.Lerp(palettes[currentIndex].ambientColor,
                       palettes[nextIndex].ambientColor, t);

        RenderSettings.fogColor =
            Color.Lerp(palettes[currentIndex].fogColor,
                       palettes[nextIndex].fogColor, t);

        dirLight.color =
            Color.Lerp(palettes[currentIndex].lightColor,
                       palettes[nextIndex].lightColor, t);

        if (t >= 1f)
        {
            t = 0f;
            currentIndex = nextIndex;
            nextIndex = (nextIndex + 1) % palettes.Length;
        }
    }

    void ApplyPalette(Palette p)
    {
        RenderSettings.ambientLight = p.ambientColor;
        RenderSettings.fogColor = p.fogColor;
        dirLight.color = p.lightColor;
    }
}
