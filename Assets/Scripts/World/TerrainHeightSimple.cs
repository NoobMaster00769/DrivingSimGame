using UnityEngine;

[RequireComponent(typeof(Terrain), typeof(TerrainCollider))]
public class TerrainHeightSimple : MonoBehaviour
{
    [Header("Global Noise")]
    [Tooltip("Lower = wider hills (0.0008–0.002 recommended)")]
    public float noiseScale = 0.0012f;

    [Tooltip("World-space meters")]
    public float heightAmplitude = 60f;

    Terrain terrain;
    TerrainCollider terrainCollider;

    void Awake()
    {
        terrain = GetComponent<Terrain>();
        terrainCollider = GetComponent<TerrainCollider>();
    }

    void Start()
    {
        ApplyHeight();
    }

    void ApplyHeight()
    {
        TerrainData data = terrain.terrainData;
        int res = data.heightmapResolution;

        float[,] heights = new float[res, res];

        float terrainHeight = data.size.y;
        Vector3 terrainPos = terrain.transform.position;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float worldX =
                    terrainPos.x + (float)x / (res - 1) * data.size.x;

                float worldZ =
                    terrainPos.z + (float)z / (res - 1) * data.size.z;

                float n = Mathf.PerlinNoise(
                    worldX * noiseScale,
                    worldZ * noiseScale
                );

                heights[z, x] = (n * heightAmplitude) / terrainHeight;
            }
        }

        data.SetHeights(0, 0, heights);

        // Force collider refresh
        terrainCollider.terrainData = null;
        terrainCollider.terrainData = data;
    }
}
