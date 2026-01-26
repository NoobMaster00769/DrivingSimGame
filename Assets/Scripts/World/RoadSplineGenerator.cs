using UnityEngine;
using System.Collections.Generic;

public class RoadSplineGenerator : MonoBehaviour
{
    [Header("References")]
    public ProceduralTerrain terrain;

    [Header("Road Length")]
    public int pointCount = 300;
    public float segmentLength = 5f;

    [Header("Curvature")]
    public float maxTurnPerStep = 2.5f;
    public float curvatureScale = 0.005f;

    [Header("Height")]
    public float roadHeightOffset = 0.05f;

    public List<Vector3> points = new();

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        points.Clear();

        Vector3 pos = terrain.transform.position +
            new Vector3(
                terrain.xSize * terrain.vertexSpacing * 0.5f,
                0f,
                terrain.vertexSpacing
            );

        float heading = 0f;

        for (int i = 0; i < pointCount; i++)
        {
            // ✅ SAMPLE TERRAIN HEIGHT ONCE
            float terrainHeight = SampleTerrainHeight(pos);
            pos.y = terrainHeight + roadHeightOffset;

            points.Add(pos);

            float noise = Mathf.PerlinNoise(i * curvatureScale, 0f);
            float turn = Mathf.Lerp(-maxTurnPerStep, maxTurnPerStep, noise);
            heading += turn;

            Vector3 dir = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
            pos += dir.normalized * segmentLength;

            if (!IsInsideTerrain(pos))
                break;
        }
    }

    float SampleTerrainHeight(Vector3 worldPos)
    {
        Mesh m = terrain.GetComponent<MeshFilter>().sharedMesh;
        if (m == null) return worldPos.y;

        Vector3 local = terrain.transform.InverseTransformPoint(worldPos);

        int x = Mathf.RoundToInt(local.x / terrain.vertexSpacing);
        int z = Mathf.RoundToInt(local.z / terrain.vertexSpacing);

        x = Mathf.Clamp(x, 0, terrain.xSize);
        z = Mathf.Clamp(z, 0, terrain.zSize);

        int idx = z * (terrain.xSize + 1) + x;
        idx = Mathf.Clamp(idx, 0, m.vertexCount - 1);

        return terrain.transform.TransformPoint(m.vertices[idx]).y;
    }

    bool IsInsideTerrain(Vector3 worldPos)
    {
        Vector3 l = terrain.transform.InverseTransformPoint(worldPos);

        return
            l.x >= 2 &&
            l.z >= 2 &&
            l.x <= terrain.xSize * terrain.vertexSpacing - 2 &&
            l.z <= terrain.zSize * terrain.vertexSpacing - 2;
    }
}
