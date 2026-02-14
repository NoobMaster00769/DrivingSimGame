using UnityEngine;

public class WorldEventDirector : MonoBehaviour
{
    public RoadState road;

    [Header("Arc Settings")]
    public float arcDuration = 600f; // 10 minutes
    public float blendSpeed = 0.6f;

    float arcTimer;

    float arcCurvatureBias;
    float arcWidthBias;
    float arcBankBias;

    float targetCurvature = 1f;
    float targetWidth = 1f;
    float targetBanking = 1f;

    void Start()
    {
        BeginNewArc();
    }

    void Update()
    {
        if (!road) return;

        arcTimer += Time.deltaTime;

        if (arcTimer > arcDuration)
            BeginNewArc();

        UpdateArcPhases();
        UpdateMultipliers();
    }

    void BeginNewArc()
    {
        arcTimer = 0f;

        // Slight randomness per arc
        arcCurvatureBias = Random.Range(0.9f, 1.15f);
        arcWidthBias = Random.Range(0.9f, 1.15f);
        arcBankBias = Random.Range(0.9f, 1.2f);
    }

    void UpdateArcPhases()
    {
        float t = arcTimer / arcDuration;

        // 5 Phases
        if (t < 0.2f)
        {
            // STILL
            targetCurvature = 0.9f;
            targetWidth = 1.15f;
            targetBanking = 0.9f;
        }
        else if (t < 0.4f)
        {
            // WIDE DRIFT
            targetCurvature = 0.95f;
            targetWidth = 1.2f;
            targetBanking = 0.95f;
        }
        else if (t < 0.6f)
        {
            // SERPENTINE
            targetCurvature = 1.2f;
            targetWidth = 1.0f;
            targetBanking = 1.15f;
        }
        else if (t < 0.8f)
        {
            // COMPRESSED INTENSITY
            targetCurvature = 1.35f;
            targetWidth = 0.85f;
            targetBanking = 1.25f;
        }
        else
        {
            // DISSOLVE / RETURN
            targetCurvature = 0.85f;
            targetWidth = 1.25f;
            targetBanking = 0.85f;
        }

        targetCurvature *= arcCurvatureBias;
        targetWidth *= arcWidthBias;
        targetBanking *= arcBankBias;
    }

    void UpdateMultipliers()
    {
        road.curvatureMultiplier =
            Mathf.Lerp(road.curvatureMultiplier,
                       targetCurvature,
                       Time.deltaTime * blendSpeed);

        road.widthMultiplier =
            Mathf.Lerp(road.widthMultiplier,
                       targetWidth,
                       Time.deltaTime * blendSpeed);

        road.bankingMultiplier =
            Mathf.Lerp(road.bankingMultiplier,
                       targetBanking,
                       Time.deltaTime * blendSpeed);
    }
}
