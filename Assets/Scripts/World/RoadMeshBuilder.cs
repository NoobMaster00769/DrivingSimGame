using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class RoadMeshBuilder : MonoBehaviour
{
    [Header("References")]
    public RoadSplineGenerator spline;
    public ProceduralTerrain terrain;

    [Header("Road Shape")]
    public float roadWidth = 6f;
    public float roadYOffset = 0.15f;

    [Header("Banking")]
    public float maxBankAngle = 8f;

    Mesh mesh;

    void Start()
    {
        StartCoroutine(BuildWhenReady());
    }

    IEnumerator BuildWhenReady()
    {
        // Wait for spline
        while (spline == null || spline.points == null || spline.points.Count < 4)
            yield return null;

        // Wait for terrain mesh
        MeshFilter mf = null;
        while (terrain == null ||
               (mf = terrain.GetComponent<MeshFilter>()) == null ||
               mf.sharedMesh == null ||
               mf.sharedMesh.vertexCount == 0)
        {
            yield return null;
        }

        BuildRoad();
    }

    void BuildRoad()
    {
        mesh = new Mesh { name = "Road Mesh" };

        List<Vector3> verts = new();
        List<int> tris = new();

        int vi = 0;

        for (int i = 1; i < spline.points.Count - 1; i++)
        {
            Vector3 prev = spline.points[i - 1];
            Vector3 cur = spline.points[i];
            Vector3 next = spline.points[i + 1];

            Vector3 forward = (next - cur).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            // ---- BANKING ----
            float curve = Vector3.SignedAngle(
                (cur - prev).normalized,
                (next - cur).normalized,
                Vector3.up
            );

            float bank = Mathf.Clamp(curve * 0.5f, -maxBankAngle, maxBankAngle);
            Vector3 bankedRight = Quaternion.AngleAxis(bank, forward) * right;

            Vector3 left = cur - bankedRight * (roadWidth * 0.5f);
            Vector3 rightPos = cur + bankedRight * (roadWidth * 0.5f);

            left.y = SampleTerrainHeight(left) + roadYOffset;
            rightPos.y = SampleTerrainHeight(rightPos) + roadYOffset;

            verts.Add(left);
            verts.Add(rightPos);

            if (vi >= 2)
            {
                tris.Add(vi - 2);
                tris.Add(vi - 1);
                tris.Add(vi);

                tris.Add(vi);
                tris.Add(vi - 1);
                tris.Add(vi + 1);
            }

            vi += 2;
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;

        // ---- DOUBLE SIDED FIX ----
        int originalVertCount = verts.Count;
        int originalTriCount = tris.Count;

        for (int i = 0; i < originalVertCount; i++)
            verts.Add(verts[i]);

        for (int i = 0; i < originalTriCount; i += 3)
        {
            tris.Add(tris[i + 2] + originalVertCount);
            tris.Add(tris[i + 1] + originalVertCount);
            tris.Add(tris[i] + originalVertCount);
        }

    }

    float SampleTerrainHeight(Vector3 worldPos)
    {
        if (terrain == null)
            return worldPos.y;

        MeshFilter mf = terrain.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
            return worldPos.y;

        Mesh m = mf.sharedMesh;

        Vector3 local = terrain.transform.InverseTransformPoint(worldPos);

        float maxX = terrain.xSize * terrain.vertexSpacing;
        float maxZ = terrain.zSize * terrain.vertexSpacing;

        if (local.x < 0 || local.z < 0 || local.x > maxX || local.z > maxZ)
            return worldPos.y;

        int x = Mathf.RoundToInt(local.x / terrain.vertexSpacing);
        int z = Mathf.RoundToInt(local.z / terrain.vertexSpacing);

        int idx = z * (terrain.xSize + 1) + x;
        idx = Mathf.Clamp(idx, 0, m.vertexCount - 1);

        return terrain.transform.TransformPoint(m.vertices[idx]).y;
    }

}
