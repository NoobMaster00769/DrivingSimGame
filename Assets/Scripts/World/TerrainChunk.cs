using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainChunk : MonoBehaviour
{
    [Header("Terrain Shape")]
    public int size = 20;
    public float vertexSpacing = 1f;
    public float heightScale = 2f;
    public float noiseScale = 0.1f;

    [Header("Road Influence")]
    public Transform roadCenter;
    public float roadWidth = 6f;
    public float blendDistance = 8f;
    public float roadHeight = 0f;

    public Gradient gradient;

    Mesh mesh;

    void Awake()
    {
        Generate();
    }

    void Generate()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        int vertsPerLine = size + 1;
        Vector3[] verts = new Vector3[vertsPerLine * vertsPerLine];
        Color[] colors = new Color[verts.Length];
        int[] tris = new int[size * size * 6];

        int i = 0;
        for (int z = 0; z <= size; z++)
        {
            for (int x = 0; x <= size; x++)
            {
                Vector3 worldPos =
                    transform.position +
                    new Vector3(x * vertexSpacing, 0f, z * vertexSpacing);

                float noise =
                    Mathf.PerlinNoise(
                        worldPos.x * noiseScale,
                        worldPos.z * noiseScale
                    );

                float terrainHeight = noise * heightScale;

                float finalHeight = terrainHeight;

                if (roadCenter)
                {
                    float lateralDist =
                        Vector3.Distance(
                            new Vector3(worldPos.x, 0, worldPos.z),
                            new Vector3(roadCenter.position.x, 0, roadCenter.position.z)
                        );

                    float t =
                        Mathf.InverseLerp(
                            roadWidth * 0.5f,
                            roadWidth * 0.5f + blendDistance,
                            lateralDist
                        );

                    finalHeight = Mathf.Lerp(roadHeight, terrainHeight, t);
                }

                verts[i] = new Vector3(x * vertexSpacing, finalHeight, z * vertexSpacing);
                colors[i] = gradient.Evaluate(noise);
                i++;
            }
        }

        int ti = 0;
        int vi = 0;
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                tris[ti++] = vi;
                tris[ti++] = vi + vertsPerLine;
                tris[ti++] = vi + 1;

                tris[ti++] = vi + 1;
                tris[ti++] = vi + vertsPerLine;
                tris[ti++] = vi + vertsPerLine + 1;

                vi++;
            }
            vi++;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.colors = colors;
        mesh.RecalculateNormals();

        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}
