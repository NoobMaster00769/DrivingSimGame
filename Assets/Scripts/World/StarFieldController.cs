using UnityEngine;

public class StarfieldController : MonoBehaviour
{
    public RoadState roadState;
    public PlayerDriveMetrics metrics;
    public ParticleSystem stars;

    ParticleSystem.MainModule main;
    ParticleSystem.EmissionModule emission;

    void Start()
    {
        main = stars.main;
        emission = stars.emission;
    }

    void Update()
    {
        float mood = roadState.tempest;

        main.startSize =
            Mathf.Lerp(0.05f, 0.15f, mood);

        emission.rateOverTime =
            Mathf.Lerp(60f, 140f, mood);

        float starSpeed =
            Mathf.Lerp(0.1f, 1.2f, metrics.intensity);

        main.startSpeed = starSpeed;
    }
}