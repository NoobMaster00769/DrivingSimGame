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

    float timeAccumulator;

    // Section system
    float sectionTimer;
    float sectionDuration;
    int sectionType;
    // 0 = Wide Flow
    // 1 = S Curves
    // 2 = Tight Technical
    // 3 = High Speed Run

    void Start()
    {
        PickNewSection();
    }

    void Update()
    {
        if (!player) return;

        timeAccumulator += Time.deltaTime;
        sectionTimer += Time.deltaTime;

        if (sectionTimer > sectionDuration)
            PickNewSection();

        UpdateGeometry();
    }

    void PickNewSection()
    {
        sectionTimer = 0f;
        sectionDuration = Random.Range(7f, 16f);

        sectionType = Random.Range(0, 4);
    }

    void UpdateGeometry()
    {
        float t = timeAccumulator;

        switch (sectionType)
        {
            // -----------------------------
            // 0 — WIDE FLOWING HIGHWAY
            // -----------------------------
            case 0:
                curvature =
                    Mathf.Clamp01(
                        0.5f +
                        Mathf.Sin(t * 0.5f) * 0.2f
                    );

                width = 0.8f;

                banking =
                    Mathf.Clamp01(
                        0.5f +
                        Mathf.Sin(t * 0.6f) * 0.15f
                    );
                break;

            // -----------------------------
            // 1 — RHYTHMIC S-CURVES
            // -----------------------------
            case 1:
                curvature =
                    Mathf.Clamp01(
                        0.5f +
                        Mathf.Sin(t * 1.2f) * 0.35f
                    );

                width = 0.65f;

                banking =
                    Mathf.Clamp01(
                        0.5f +
                        Mathf.Sin(t * 1.2f) * 0.3f
                    );
                break;

            // -----------------------------
            // 2 — TIGHT TECHNICAL
            // -----------------------------
            case 2:
                curvature =
                    Mathf.Clamp01(
                        0.5f +
                        Mathf.PerlinNoise(t * 1.8f, 1f) * 0.6f
                    );

                width = 0.55f;

                banking =
                    Mathf.Clamp01(
                        0.5f +
                        Mathf.PerlinNoise(t * 1.5f, 2f) * 0.4f
                    );
                break;

            // -----------------------------
            // 3 — HIGH SPEED STRAIGHT
            // -----------------------------
            case 3:
                curvature =
                    Mathf.Clamp01(
                        0.5f +
                        Mathf.Sin(t * 0.3f) * 0.1f
                    );

                width = 0.9f;

                banking =
                    Mathf.Clamp01(
                        0.5f +
                        Mathf.Sin(t * 0.4f) * 0.1f
                    );
                break;
        }

        // Elevation now purely aesthetic
        elevationEnergy =
            0.5f +
            Mathf.Sin(Time.time * 0.15f) * 0.25f;
    }
}
