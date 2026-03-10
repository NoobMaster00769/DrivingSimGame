using UnityEngine;

public class ChunkDistanceFade : MonoBehaviour
{
    public Transform player;
    public float maxDistance = 120f;

    ParticleSystem[] systems;
    float[] baseRates;

    void Start()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        baseRates = new float[systems.Length];

        for (int i = 0; i < systems.Length; i++)
        {
            var emission = systems[i].emission;
            baseRates[i] = emission.rateOverTime.constant;
        }
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(player.position, transform.position);
        float t = Mathf.Clamp01(1f - (dist / maxDistance));

        float intensity = Mathf.Pow(t, 0.6f);
        float brightnessBoost = Mathf.Lerp(1f, 1.6f, intensity);

        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];

            // emission fade
            var emission = ps.emission;
            emission.rateOverTime =
                new ParticleSystem.MinMaxCurve(baseRates[i] * intensity * brightnessBoost);

            // subtle cosmic drift
            var noise = ps.noise;
            noise.enabled = true;

            noise.strength = Mathf.Lerp(0.05f, 0.18f, 1f - intensity);
            noise.frequency = 0.25f;

            // very slow backward flow so stars appear to materialize forward
            noise.scrollSpeed = Mathf.Lerp(0.02f, 0.12f, 1f - intensity);

            // forward spawn shift
            var shape = ps.shape;

            float length = shape.scale.z;

            shape.position = new Vector3(
                0f,
                0f,
                Mathf.Lerp(length * 0.45f, 0f, intensity)
            );
        }
    }
}