using UnityEngine;

public class RoadState : MonoBehaviour
{
    public PlayerDriveMetrics player;

    [Range(0, 1)] public float agitation;

    public float riseSpeed = 0.15f;
    public float fallSpeed = 0.35f;

    [Range(0, 1)] public float noHelpThreshold = 0.75f;

    // OUTPUTS
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
        if (player.aggression > 0.3f)
            agitation += player.aggression * riseSpeed * Time.deltaTime;
        else
            agitation -= fallSpeed * Time.deltaTime;

        agitation = Mathf.Clamp01(agitation);
    }

    void UpdateRoadIntent()
    {
        float help =
            agitation < noHelpThreshold
            ? 1f - agitation / noHelpThreshold
            : 0f;

        // Calm → flowing | Aggressive → tight
        curvature = Mathf.Lerp(0.2f, 1.0f, agitation);

        // Calm → wide | Aggressive → narrow
        width = Mathf.Lerp(1.0f, 0.4f, agitation * help);

        // Calm → bank helps | Aggressive → flat
        banking = Mathf.Lerp(1.0f, 0.3f, agitation * help);
    }
}
