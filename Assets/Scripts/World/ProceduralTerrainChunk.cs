using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralTerrain : MonoBehaviour
{
    [Header("Terrain Size")]
    public int width = 100;
    public int length = 100;
    public float scale = 20f;

    [Header("Height")]
    public float heightMultiplier = 10f;
    public float noiseScale = 0.1f;
    public Vector2 noiseOffset;

    [Header("Shader Sync")]
    public Material terrainMaterial;

    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    void Start()
    {
        Generate();
    }

    void Generate()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";

        GetComponent<MeshFilter>().mesh = mesh;

        CreateVertices();
        CreateTriangles();
        UpdateMesh();
    }

    void CreateVertices()
    {
        vertices = new Vector3[(width + 1) * (length + 1)];

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        for (int z = 0; z <= length; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float xCoord = (x + noiseOffset.x) * noiseScale;
                float zCoord = (z + noiseOffset.y) * noiseScale;

                float y = Mathf.PerlinNoise(xCoord, zCoord) * heightMultiplier;

                float centeredX = x - width / 2f;
                float centeredZ = z - length / 2f;

                vertices[z * (width + 1) + x] = new Vector3(centeredX, y, centeredZ);


                minHeight = Mathf.Min(minHeight, y);
                maxHeight = Mathf.Max(maxHeight, y);
            }
        }

        // 🔗 Send height info to shader
        if (terrainMaterial != null)
        {
            terrainMaterial.SetFloat("_minHeight", minHeight);
            terrainMaterial.SetFloat("_maxHeight", maxHeight);
        }
    }

    void CreateTriangles()
    {
        triangles = new int[width * length * 6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                triangles[tris + 0] = vert;
                triangles[tris + 1] = vert + width + 1;
                triangles[tris + 2] = vert + 1;

                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + width + 1;
                triangles[tris + 5] = vert + width + 2;

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
        mesh.RecalculateNormals();

        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}
