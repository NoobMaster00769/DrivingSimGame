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

    [HideInInspector] public float curvatureMultiplier = 1f;
    [HideInInspector] public float widthMultiplier = 1f;
    [HideInInspector] public float bankingMultiplier = 1f;

    float sectionTimer;
    float sectionDuration;
    int sectionType;

    float sectionTime;              // resets per section
    float blendSpeed = 1.5f;

    float targetCurvature;
    float targetWidth;
    float targetBanking;

    void Start()
    {
        PickNewSection();
    }

    void Update()
    {
        if (!player) return;

        sectionTimer += Time.deltaTime;
        sectionTime += Time.deltaTime;

        if (sectionTimer > sectionDuration)
            PickNewSection();

        UpdateGeometry();
    }

    void PickNewSection()
    {
        sectionTimer = 0f;
        sectionTime = 0f;
        sectionDuration = Random.Range(8f, 18f);

        sectionType = Random.Range(0, 4);
    }

    void UpdateGeometry()
    {
        float t = sectionTime;

        switch (sectionType)
        {
            // -----------------------------
            // 0 — WIDE FLOWING HIGHWAY
            // -----------------------------
            case 0:
                targetCurvature =
                    0.5f + Mathf.Sin(t * 0.5f) * 0.2f;

                targetWidth = 0.8f;

                targetBanking =
                    0.5f + Mathf.Sin(t * 0.6f) * 0.15f;
                break;

            // -----------------------------
            // 1 — RHYTHMIC S-CURVES
            // -----------------------------
            case 1:
                targetCurvature =
                    0.5f + Mathf.Sin(t * 1.2f) * 0.35f;

                targetWidth = 0.65f;

                targetBanking =
                    0.5f + Mathf.Sin(t * 1.2f) * 0.3f;
                break;

            // -----------------------------
            // 2 — TIGHT TECHNICAL
            // -----------------------------
            case 2:
                targetCurvature =
                    0.5f + Mathf.PerlinNoise(t * 1.8f, 1f) * 0.6f;

                targetWidth = 0.55f;

                targetBanking =
                    0.5f + Mathf.PerlinNoise(t * 1.5f, 2f) * 0.4f;
                break;

            // -----------------------------
            // 3 — HIGH SPEED STRAIGHT
            // -----------------------------
            case 3:
                targetCurvature =
                    0.5f + Mathf.Sin(t * 0.3f) * 0.1f;

                targetWidth = 0.9f;

                targetBanking =
                    0.5f + Mathf.Sin(t * 0.4f) * 0.1f;
                break;
        }

        // Smooth morphing instead of snapping
        curvature = Mathf.Lerp(curvature, targetCurvature, Time.deltaTime * blendSpeed);
        width = Mathf.Lerp(width, targetWidth, Time.deltaTime * blendSpeed);
        banking = Mathf.Lerp(banking, targetBanking, Time.deltaTime * blendSpeed);

        // Elevation purely aesthetic
        elevationEnergy =
            0.5f +
            Mathf.Sin(Time.time * 0.15f) * 0.25f;

        // Apply multipliers
        curvature *= curvatureMultiplier;
        width *= widthMultiplier;
        banking *= bankingMultiplier;

        curvature = Mathf.Clamp01(curvature);
        width = Mathf.Clamp01(width);
        banking = Mathf.Clamp01(banking);
    }
}
