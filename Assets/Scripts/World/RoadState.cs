using UnityEngine;

public class RoadState : MonoBehaviour
{
    public PlayerDriveMetrics player;

    [Range(0, 1)] public float agitation;

    [Header("Rates")]
    public float riseSpeed = 0.2f;
    public float fallSpeed = 0.4f;

    [Header("Outputs")]
    [Range(0, 1)] public float curvature;
    [Range(0, 1)] public float width;
    [Range(0, 1)] public float banking;

    void Update()
    {
        if (!player) return;

        UpdateAgitation();
        UpdateRoadIntent();
    }

    void UpdateAgitation()
    {
        float stress =
            player.aggression +
            player.engineStress * 0.8f +
            player.gearMistake * 0.6f;

        if (stress > 0.4f)
            agitation += stress * riseSpeed * Time.deltaTime;
        else
            agitation -= fallSpeed * Time.deltaTime;

        agitation = Mathf.Clamp01(agitation);
    }

    void UpdateRoadIntent()
    {
        // FLOW comes from mechanical harmony
        float harmony =
            player.smoothness *
            (1f - player.engineStress) *
            (1f - player.gearMistake);

        // CURVATURE
        curvature = Mathf.Lerp(
            0.25f,   // calm flow
            0.9f,    // tight turns
            agitation
        );

        // WIDTH
        width = Mathf.Lerp(
            1.0f,    // wide
            0.5f,    // narrow
            player.gearMistake
        );

        // BANKING
        banking = Mathf.Lerp(
            0.9f * harmony, // only helps when calm
            0.2f,
            agitation
        );
    }
}
