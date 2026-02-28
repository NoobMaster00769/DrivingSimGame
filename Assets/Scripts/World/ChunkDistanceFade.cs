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

        // curve it so it ramps stronger near player
        float intensity = Mathf.Pow(t, 0.6f);   // <--- makes it appear earlier

        float brightnessBoost = Mathf.Lerp(1f, 1.6f, intensity);
        // 1.6 = 60% brighter when close

        for (int i = 0; i < systems.Length; i++)
        {
            var emission = systems[i].emission;
            emission.rateOverTime =
                new ParticleSystem.MinMaxCurve(baseRates[i] * intensity * brightnessBoost);
        }
    }
}