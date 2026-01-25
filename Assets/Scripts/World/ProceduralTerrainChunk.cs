using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralTerrain : MonoBehaviour
{
    Mesh mesh;

    Vector3[] vertices;
    int[] triangles;
    Color[] colors;

    [Header("Terrain Size")]
    public int xSize = 120;
    public int zSize = 120;
    public float vertexSpacing = 1f;

    [Header("Noise Layers (CALM)")]
    public float landmassScale = 0.015f;   // large shapes
    public float landmassAmp = 18f;

    public float hillScale = 0.05f;        // rolling hills
    public float hillAmp = 6f;

    public float detailScale = 0.15f;      // subtle bumps
    public float detailAmp = 1.2f;

    [Header("Height Shaping")]
    [Range(0.2f, 1.5f)]
    public float heightCurvePower = 0.65f;

    [Header("Road Blending")]
    public RoadSplineGenerator roadSpline;
    public float roadBlendRadius = 10f;     // how wide terrain reacts
    public float roadBlendStrength = 0.6f;  // how deep it blends

    [Header("Color")]
    public Gradient gradient;

    float minTerrainHeight;
    float maxTerrainHeight;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";
        GetComponent<MeshFilter>().mesh = mesh;

        Generate();
    }

    void Generate()
    {
        CreateShape();
        CreateTriangles();
        UpdateMesh();
    }

    void CreateShape()
    {
        minTerrainHeight = float.MaxValue;
        maxTerrainHeight = float.MinValue;

        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        int i = 0;
        for (int z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float height = GetNoiseSample(x, z);

                // --- ROAD INFLUENCE ---
                if (roadSpline != null && roadSpline.points.Count > 0)
                {
                    Vector2 terrainPos = new Vector2(
                        x * vertexSpacing,
                        z * vertexSpacing
                    );

                    float closestDist = float.MaxValue;

                    foreach (Vector3 rp in roadSpline.points)
                    {
                        Vector3 localRoadPoint =
                            transform.InverseTransformPoint(rp);

                        float d = Vector2.Distance(
                            terrainPos,
                            new Vector2(localRoadPoint.x, localRoadPoint.z)
                        );

                        if (d < closestDist)
                            closestDist = d;
                    }

                    if (closestDist < roadBlendRadius)
                    {
                        float t = 1f - Mathf.SmoothStep(
                            0f,
                            roadBlendRadius,
                            closestDist
                        );

                        height -= t * roadBlendStrength * landmassAmp;
                    }
                }

                vertices[i] = new Vector3(
                    x * vertexSpacing,
                    height,
                    z * vertexSpacing
                );

                minTerrainHeight = Mathf.Min(minTerrainHeight, height);
                maxTerrainHeight = Mathf.Max(maxTerrainHeight, height);

                i++;
            }
        }

        colors = new Color[vertices.Length];
        for (i = 0; i < vertices.Length; i++)
        {
            float h = Mathf.InverseLerp(
                minTerrainHeight,
                maxTerrainHeight,
                vertices[i].y
            );
            colors[i] = gradient.Evaluate(h);
        }
    }

    void CreateTriangles()
    {
        triangles = new int[xSize * zSize * 6];

        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;

                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();

        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    float GetNoiseSample(int x, int z)
    {
        float nx = x * vertexSpacing;
        float nz = z * vertexSpacing;
        float flowBias = z * 0.002f;

        float landmass =
            Mathf.PerlinNoise(nx * landmassScale, (nz + flowBias) * landmassScale)
            * landmassAmp;

        float hills =
            Mathf.PerlinNoise(nx * hillScale, (nz + flowBias) * hillScale)
            * hillAmp;

        float detail =
            Mathf.PerlinNoise(nx * detailScale, nz * detailScale)
            * detailAmp;

        float rawHeight = landmass + hills + detail;

        float maxPossible = landmassAmp + hillAmp + detailAmp;
        float normalized = Mathf.Clamp01(rawHeight / maxPossible);

        // Flattens valleys, keeps peaks rare → calm driving
        normalized = Mathf.Pow(normalized, heightCurvePower);

        return normalized * landmassAmp;
    }
}
