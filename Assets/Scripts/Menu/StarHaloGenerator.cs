using UnityEngine;
using System.Collections.Generic;

public class StarHaloGenerator : MonoBehaviour
{
    public GameObject starPrefab;
    
    float pulse;

    [Header("Halo Structure")]
    public int starsPerLayer = 8;

    public float innerRadius = 4f;
    public float midRadius = 6f;
    public float outerRadius = 8f;

    [Header("Organic Noise")]
    public float noiseAmount = 0.6f;

    [Header("Rotation")]
    public float innerSpeed = 80f;
    public float midSpeed = 40f;
    public float outerSpeed = 20f;

    Transform innerLayer;
    Transform midLayer;
    Transform outerLayer;

    LineRenderer constellationLine;

    List<Transform> midStars = new();

    void Start()
    {
        innerLayer = new GameObject("InnerHalo").transform;
        midLayer = new GameObject("MidHalo").transform;
        outerLayer = new GameObject("OuterHalo").transform;

        innerLayer.SetParent(transform);
        midLayer.SetParent(transform);
        outerLayer.SetParent(transform);

        innerLayer.localPosition = Vector3.zero;
        midLayer.localPosition = Vector3.zero;
        outerLayer.localPosition = Vector3.zero;

        GenerateLayer(innerLayer, innerRadius, starsPerLayer);
        GenerateLayer(midLayer, midRadius, starsPerLayer, true);
        GenerateLayer(outerLayer, outerRadius, starsPerLayer);

        CreateConstellationLines();
    }

    void GenerateLayer(Transform parent, float radius, int count, bool storeStars = false)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;

            float noise =
                Mathf.PerlinNoise(i * 0.3f, radius) * noiseAmount;

            float r = radius + noise;

            Vector3 pos =
                new Vector3(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle),
                    0
                ) * r;

            GameObject star =
                Instantiate(starPrefab, parent);

            star.transform.localPosition = pos;

            star.transform.localScale *= Random.Range(0.7f, 1.3f);

            if (storeStars)
                midStars.Add(star.transform);
        }
    }

    void CreateConstellationLines()
    {
        constellationLine = gameObject.AddComponent<LineRenderer>();

        constellationLine.useWorldSpace = true;

        constellationLine.material =
            new Material(Shader.Find("Sprites/Default"));

        constellationLine.widthMultiplier = 0.05f;

        constellationLine.positionCount = midStars.Count;

        constellationLine.startColor = new Color(1, 1, 1, 0);
        constellationLine.endColor = new Color(1, 1, 1, 0);
    }

    void Update()
    {

        if (pulse > 0)
        {
            pulse -= Time.deltaTime * 4f;

            float scale = 1f + pulse * 0.25f;

            transform.localScale = Vector3.one * scale;
        }
        else
        {
            transform.localScale =
                Vector3.Lerp(
                    transform.localScale,
                    Vector3.one,
                    Time.deltaTime * 4f
                );
        }

        float wobble =
    Mathf.Sin(Time.time * 0.6f) * 0.03f;

        transform.localScale =
            Vector3.one * (1 + wobble);
        innerLayer.Rotate(Vector3.forward * innerSpeed * Time.deltaTime);
        midLayer.Rotate(Vector3.forward * midSpeed * Time.deltaTime);
        outerLayer.Rotate(Vector3.forward * outerSpeed * Time.deltaTime);

        UpdateConstellation();
    }

    void UpdateConstellation()
    {
        if (constellationLine == null) return;

        for (int i = 0; i < midStars.Count; i++)
        {
            constellationLine.SetPosition(i, midStars[i].position);
        }
    }

    public void Pulse()
    {
        pulse = 1f;
    }

    public void Highlight(bool active)
    {
        innerSpeed = active ? 120f : 80f;
        midSpeed = active ? 60f : 40f;

        if (constellationLine == null) return;

        Color c =
            active
            ? new Color(1, 1, 1, 0.6f)
            : new Color(1, 1, 1, 0);

        constellationLine.startColor = c;
        constellationLine.endColor = c;
    }
}