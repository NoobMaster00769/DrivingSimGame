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

        float intensity = Mathf.SmoothStep(0f, 1f, t);
        float brightnessBoost = Mathf.Lerp(1f, 1.6f, intensity);

        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];

            // emission fade
            var emission = ps.emission;
            emission.rateOverTime =
                new ParticleSystem.MinMaxCurve(baseRates[i] * intensity * brightnessBoost);


            // ⭐ cosmic drift
            var noise = ps.noise;
            noise.enabled = true;

            noise.strength = Mathf.Lerp(0.04f, 0.12f, 1f - intensity);
            noise.frequency = 0.25f;
            noise.scrollSpeed = Mathf.Lerp(0.02f, 0.08f, 1f - intensity);

            // ⭐ forward painting illusion
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