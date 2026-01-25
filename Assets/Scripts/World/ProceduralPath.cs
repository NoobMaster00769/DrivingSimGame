using UnityEngine;
using System.Collections.Generic;

public class ProceduralRoadPath : MonoBehaviour
{
    [Header("References")]
    public ProceduralTerrain terrain;

    [Header("Road Shape")]
    public int segmentCount = 200;
    public float segmentLength = 6f;

    [Range(0f, 1f)]
    public float turnStrength = 0.25f;

    [Range(0f, 1f)]
    public float valleyBias = 0.5f;

    [Header("Sampling")]
    public float valleySampleOffset = 4f;
    public float roadHeightOffset = 0.15f;

    [Header("Debug")]
    public bool drawGizmos = true;

    public List<Vector3> roadPoints = new List<Vector3>();

    void Start()
    {
        GeneratePath();
    }

    void GeneratePath()
    {
        roadPoints.Clear();

        Vector3 currentPos = new Vector3(
            terrain.xSize * 0.5f,
            0f,
            1f
        );

        Vector3 direction = Vector3.forward;

        for (int i = 0; i < segmentCount; i++)
        {
            if (!IsInsideTerrain(currentPos))
                break;

            float center = SampleTerrainHeightSafe(currentPos);

            float left = SampleTerrainHeightSafe(
                currentPos - Vector3.right * valleySampleOffset
            );

            float right = SampleTerrainHeightSafe(
                currentPos + Vector3.right * valleySampleOffset
            );

            float valleySlope = Mathf.Clamp(left - right, -1f, 1f);
            float valleyTurn = valleySlope * valleyBias * 10f;

            float noise = Mathf.PerlinNoise(i * 0.12f, 0f);
            float drift = Mathf.Lerp(-1f, 1f, noise) * turnStrength * 10f;

            float finalTurn = valleyTurn + drift;

            direction = Quaternion.Euler(0f, finalTurn, 0f) * direction;
            direction.Normalize();

            currentPos.y = center + roadHeightOffset;
            roadPoints.Add(currentPos);

            currentPos += direction * segmentLength;
        }
    }

    bool IsInsideTerrain(Vector3 pos)
    {
        return
            pos.x >= 1 &&
            pos.z >= 1 &&
            pos.x <= terrain.xSize - 1 &&
            pos.z <= terrain.zSize - 1;
    }

    float SampleTerrainHeightSafe(Vector3 worldPos)
    {
        Mesh mesh = terrain.GetComponent<MeshFilter>().mesh;
        Vector3[] verts = mesh.vertices;

        // 🔒 Clamp POSITION first (not index)
        float clampedX = Mathf.Clamp(worldPos.x, 0, terrain.xSize);
        float clampedZ = Mathf.Clamp(worldPos.z, 0, terrain.zSize);

        int x = Mathf.RoundToInt(clampedX);
        int z = Mathf.RoundToInt(clampedZ);

        int index = z * (terrain.xSize + 1) + x;

        // Absolute safety
        if (index < 0 || index >= verts.Length)
            return 0f;

        return verts[index].y;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || roadPoints == null || roadPoints.Count < 2)
            return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < roadPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(roadPoints[i], roadPoints[i + 1]);
        }
    }
}
