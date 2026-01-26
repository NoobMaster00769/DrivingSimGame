using UnityEngine;
using System.Collections.Generic;

public class RoadSplineGenerator : MonoBehaviour
{
    [Header("Road Shape")]
    public int pointCount = 300;
    public float segmentLength = 5f;

    [Header("Curvature")]
    public float maxTurnPerStep = 2.5f;
    public float curvatureScale = 0.005f;

    [Header("Height Profile")]
    public float baseHeight = 2.5f;          // absolute world height
    public float heightNoiseScale = 0.002f;  // vertical undulation
    public float heightAmplitude = 1.5f;     // slope strength

    [Header("Debug")]
    public bool regenerateOnPlay = true;

    public List<Vector3> points = new();

    void Start()
    {
        if (regenerateOnPlay)
            Generate();
    }

    public void Generate()
    {
        points.Clear();

        Vector3 pos = transform.position;
        float heading = transform.eulerAngles.y;

        for (int i = 0; i < pointCount; i++)
        {
            // --- HEIGHT IS ROAD-CONTROLLED ---
            float heightNoise =
                Mathf.PerlinNoise(i * heightNoiseScale, 0f) - 0.5f;

            pos.y = baseHeight + heightNoise * heightAmplitude;

            points.Add(pos);

            // --- CURVATURE ---
            float curveNoise = Mathf.PerlinNoise(i * curvatureScale, 10f);
            float turn = Mathf.Lerp(-maxTurnPerStep, maxTurnPerStep, curveNoise);
            heading += turn;

            Vector3 dir = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
            pos += dir.normalized * segmentLength;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (points == null || points.Count < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 1; i < points.Count; i++)
        {
            Gizmos.DrawLine(points[i - 1], points[i]);
        }
    }
#endif
}
