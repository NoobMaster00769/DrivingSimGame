using UnityEngine;

public class RoadState : MonoBehaviour
{
    public PlayerDriveMetrics player;

    [Header("World Personality")]
    [Range(0, 1)] public float serenity;
    [Range(0, 1)] public float tempest;

    [Header("Outputs")]
    [Range(0, 1)] public float curvature;
    [Range(0, 1)] public float width;
    [Range(0, 1)] public float banking;
    [Range(0, 1)] public float elevationEnergy;

    float smoothMood;
    float timeAccumulator;

    void Update()
    {
        if (!player) return;

        timeAccumulator += Time.deltaTime;

        UpdateEmotion();
        UpdateGeometry();
    }

    void UpdateEmotion()
    {
        float calm =
            player.flow * 0.6f +
            player.controlQuality * 0.4f;

        float intense =
            player.intensity * 0.6f +
            (1f - player.controlQuality) * 0.4f;

        serenity = Mathf.Lerp(serenity, calm, Time.deltaTime * 0.4f);
        tempest = Mathf.Lerp(tempest, intense, Time.deltaTime * 0.4f);

        float mood = Mathf.Clamp01(tempest - serenity * 0.4f);

        smoothMood =
            Mathf.Lerp(smoothMood,
                       mood,
                       Time.deltaTime * 0.4f);
    }

    void UpdateGeometry()
    {
        // Faster oscillation but smaller swing
        float wave =
            Mathf.Sin(timeAccumulator * 0.6f);

        curvature =
            Mathf.Clamp01(
                0.45f +
                wave * 0.2f +
                smoothMood * 0.2f
            );

        width =
            Mathf.Clamp01(
                0.6f +
                Mathf.Sin(timeAccumulator * 0.4f) * 0.15f
            );

        banking =
            Mathf.Clamp01(
                0.5f +
                Mathf.Sin(timeAccumulator * 0.7f) * 0.25f
            );

        elevationEnergy =
    Mathf.Clamp01(
        0.4f +
        Mathf.Sin(Time.time * 0.2f) * 0.3f +
        smoothMood * 0.3f
    );

    }
}
