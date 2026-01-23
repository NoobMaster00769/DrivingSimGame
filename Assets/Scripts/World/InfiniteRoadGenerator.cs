using UnityEngine;
using System.Collections.Generic;

public class InfiniteRoadGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject roadSegmentPrefab;

    [Header("Generation Settings")]
    public int segmentsAhead = 6;
    public int segmentsBehind = 2;
    public float segmentLength = 40f;

    [Header("Curvature")]
    public float maxTurnAngle = 5f;
    public float turnSmoothing = 0.15f;

    private List<GameObject> activeSegments = new List<GameObject>();
    private Vector3 currentDirection = Vector3.forward;
    private Vector3 lastSpawnPos;

    void Start()
    {
        lastSpawnPos = Vector3.zero;

        for (int i = 0; i < segmentsAhead; i++)
            SpawnSegment();
    }

    void Update()
    {
        float distanceAhead =
            Vector3.Distance(player.position, lastSpawnPos);

        if (distanceAhead < segmentLength * segmentsAhead)
        {
            SpawnSegment();
            CleanupSegments();
        }
    }

    void SpawnSegment()
    {
        // Smooth random turn
        float turn = Random.Range(-maxTurnAngle, maxTurnAngle);
        Vector3 targetDir =
            Quaternion.Euler(0f, turn, 0f) * currentDirection;

        currentDirection =
            Vector3.Slerp(currentDirection, targetDir, turnSmoothing);

        lastSpawnPos += currentDirection.normalized * segmentLength;

        GameObject seg = Instantiate(
            roadSegmentPrefab,
            lastSpawnPos,
            Quaternion.LookRotation(currentDirection)
        );

        activeSegments.Add(seg);
    }

    void CleanupSegments()
    {
        while (activeSegments.Count >
               segmentsAhead + segmentsBehind)
        {
            Destroy(activeSegments[0]);
            activeSegments.RemoveAt(0);
        }
    }
}
