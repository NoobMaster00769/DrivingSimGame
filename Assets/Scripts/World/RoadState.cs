using UnityEngine;

public class RoadState : MonoBehaviour
{
    public PlayerDriveMetrics player;

    [Header("World Personality")]
    [Range(0, 1)] public float serenity;   // calm side
    [Range(0, 1)] public float tempest;    // intense side

    [Header("Outputs")]
    [Range(0, 1)] public float curvature;
    [Range(0, 1)] public float width;
    [Range(0, 1)] public float banking;
    [Range(0, 1)] public float elevationEnergy;

    float chapterMood;
    float smoothMood;

    void Update()
    {
        if (!player) return;

        UpdateMood();
        UpdateGeometryIntent();
    }

    void UpdateMood()
    {
        float calmInfluence =
            player.flow * 0.6f +
            player.controlQuality * 0.4f;

        float intenseInfluence =
            player.intensity * 0.7f +
            (1f - player.controlQuality) * 0.3f;

        serenity =
            Mathf.Lerp(serenity,
                       calmInfluence,
                       Time.deltaTime * 0.3f);

        tempest =
            Mathf.Lerp(tempest,
                       intenseInfluence,
                       Time.deltaTime * 0.3f);

        chapterMood =
            Mathf.Clamp01(tempest - serenity * 0.5f);

        smoothMood =
            Mathf.Lerp(smoothMood,
                       chapterMood,
                       Time.deltaTime * 0.2f);
    }

    void UpdateGeometryIntent()
    {
        // Curvature influenced by rhythm
        curvature =
            Mathf.Lerp(0.2f, 0.8f, smoothMood) *
            Mathf.Lerp(0.7f, 1.2f, player.rhythm);

        // Width
        width =
            Mathf.Lerp(1.2f, 0.7f, smoothMood);

        // Banking
        banking =
            Mathf.Lerp(0.8f, 0.2f, smoothMood);

        // Elevation intensity
        elevationEnergy =
            Mathf.Lerp(0.2f, 1f, smoothMood);
    }
}