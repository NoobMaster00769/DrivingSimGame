using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class RoadMeshBuilder : MonoBehaviour
{
    public RoadSplineGenerator spline;

    [Header("Road Shape")]
    public float roadWidth = 6f;

    [Header("Banking")]
    public float maxBankAngle = 8f;

    Mesh mesh;

    void Start()
    {
        StartCoroutine(BuildWhenReady());
    }

    IEnumerator BuildWhenReady()
    {
        while (spline == null || spline.points.Count < 4)
            yield return null;

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

            float curve =
                Vector3.SignedAngle(
                    (cur - prev).normalized,
                    (next - cur).normalized,
                    Vector3.up
                );

            float bank =
                Mathf.Clamp(curve * 0.5f, -maxBankAngle, maxBankAngle);

            Vector3 bankedRight =
                Quaternion.AngleAxis(bank, forward) * right;

            Vector3 left = cur - bankedRight * (roadWidth * 0.5f);
            Vector3 rightPos = cur + bankedRight * (roadWidth * 0.5f);

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
    }
}
