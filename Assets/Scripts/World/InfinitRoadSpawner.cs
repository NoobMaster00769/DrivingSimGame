using UnityEngine;
using System.Collections.Generic;

public class RoadSpawner : MonoBehaviour
{
    [Header("STATE (INPUT)")]
    public RoadState roadState;

    [Header("REFERENCES")]
    public Transform car;
    public RoadSegment roadPrefab;

    [Header("GENERATION")]
    public int segmentsAhead = 10;
    public float spawnAheadDistance = 70f;
    public float despawnBehindDistance = 60f;

    [Header("CURVATURE")]
    public float maxTurnAngle = 14f;

    [Header("FLOW (always active)")]
    public float flowStrength = 0.4f;
    public float flowFrequency = 0.08f;

    [Header("AGGRESSION RESPONSE")]
    public float aggressionTurnBoost = 1.2f;

    [Header("INTENT MEMORY")]
    public float intentChangeChance = 0.15f;
    public int minIntentDuration = 4;
    public int maxIntentDuration = 10;

    [Header("WIDTH")]
    public float baseWidth = 6f;
    public float maxWidthVariation = 1.5f;

    [Header("BANKING")]
    public float maxBankAngle = 8f;

    [Header("SMOOTHING")]
    public float turnSmoothSpeed = 0.35f;
    public float widthSmoothSpeed = 0.3f;
    public float bankSmoothSpeed = 0.25f;

    // ===================== INTERNAL STATE =====================

    Queue<RoadSegment> spawned = new();
    RoadSegment lastSegment;

    float currentHeading;
    float currentTurn;
    float currentTurnTarget;

    float currentWidth;
    float widthTarget;

    float currentBank;
    float bankTarget;

    int intentTimer;
    float intentDirection;

    float flowTime;

    // ===================== LIFECYCLE =====================

    void Start()
    {
        ResetState();
        SpawnInitial();
    }

    void Update()
    {
        if (!car || !lastSegment) return;

        HandleSpawning();
        HandleDespawning();
    }

    // ===================== RESET =====================

    void ResetState()
    {
        foreach (var seg in spawned)
            if (seg) Destroy(seg.gameObject);

        spawned.Clear();

        currentHeading = 0f;
        currentTurn = 0f;
        currentTurnTarget = 0f;

        currentWidth = baseWidth;
        widthTarget = baseWidth;

        currentBank = 0f;
        bankTarget = 0f;

        intentTimer = 0;
        intentDirection = Random.value < 0.5f ? -1f : 1f;

        flowTime = Random.value * 100f;
        lastSegment = null;
    }

    // ===================== SPAWNING =====================

    void HandleSpawning()
    {
        float forwardDist =
            Vector3.Distance(car.position, lastSegment.End.position);

        if (forwardDist < spawnAheadDistance)
            SpawnNext();
    }

    void HandleDespawning()
    {
        if (spawned.Count == 0) return;

        RoadSegment first = spawned.Peek();

        float behindDist =
            Vector3.Distance(car.position, first.End.position);

        if (behindDist > despawnBehindDistance)
            Destroy(spawned.Dequeue().gameObject);
    }

    void SpawnInitial()
    {
        lastSegment = Instantiate(roadPrefab, Vector3.zero, Quaternion.identity);

        // snap initial segment to terrain
        SnapSegmentToTerrain(lastSegment);

        spawned.Enqueue(lastSegment);

        for (int i = 0; i < segmentsAhead; i++)
            SpawnNext();
    }

    // ===================== CORE LOGIC =====================

    void SpawnNext()
    {
        RoadSegment next = Instantiate(roadPrefab);

        // ---------- FLOW ----------
        flowTime += flowFrequency;
        float flow =
            (Mathf.PerlinNoise(flowTime, 0f) - 0.5f) *
            maxTurnAngle *
            flowStrength;

        // ---------- INTENT ----------
        intentTimer--;
        if (intentTimer <= 0 && Random.value < intentChangeChance)
        {
            intentTimer = Random.Range(minIntentDuration, maxIntentDuration);
            intentDirection = Random.value < 0.5f ? -1f : 1f;
        }

        float intentCurve =
            intentDirection *
            Mathf.Lerp(0.3f, 1f, roadState ? roadState.curvature : 0.3f) *
            maxTurnAngle;

        // ---------- AGGRESSION ----------
        float aggressionCurve = 0f;
        if (roadState)
        {
            aggressionCurve =
                roadState.curvature *
                aggressionTurnBoost *
                maxTurnAngle *
                0.6f;
        }

        // ---------- FINAL TURN ----------
        currentTurnTarget =
            Mathf.Clamp(
                flow + intentCurve + aggressionCurve,
                -maxTurnAngle,
                maxTurnAngle
            );

        currentTurn =
            Mathf.Lerp(
                currentTurn,
                currentTurnTarget,
                turnSmoothSpeed
            );

        currentHeading += currentTurn;

        // ---------- WIDTH ----------
        if (Random.value < 0.2f)
        {
            widthTarget =
                baseWidth +
                Random.Range(-maxWidthVariation, maxWidthVariation) *
                Mathf.Lerp(1f, 0.4f, roadState ? roadState.curvature : 0f);
        }

        currentWidth =
            Mathf.Lerp(
                currentWidth,
                widthTarget,
                widthSmoothSpeed
            );

        // ---------- BANK ----------
        bankTarget =
            Mathf.Clamp(
                -currentTurn * 0.6f,
                -maxBankAngle,
                maxBankAngle
            );

        currentBank =
            Mathf.Lerp(
                currentBank,
                bankTarget,
                bankSmoothSpeed
            );

        // ===================== APPLY TRANSFORM =====================

        // yaw + bank (bank is roll, safe)
        next.transform.rotation =
            Quaternion.Euler(0f, currentHeading, currentBank);

        // perfect XZ stitching
        Vector3 offset =
            lastSegment.End.position - next.Start.position;

        offset.y = 0f;
        next.transform.position += offset;

        // snap height AFTER stitching
        SnapSegmentToTerrain(next);

        // width
        next.transform.localScale = new Vector3(
            currentWidth / baseWidth,
            1f,
            1f
        );

        spawned.Enqueue(next);
        lastSegment = next;
    }

    // ===================== TERRAIN SAMPLING =====================

    void SnapSegmentToTerrain(RoadSegment seg)
    {
        Vector3 p = seg.transform.position;
        p.y = SampleTerrainHeight(p);
        seg.transform.position = p;
    }

    float SampleTerrainHeight(Vector3 worldPos)
    {
        Terrain t = Terrain.activeTerrain;
        if (!t) return worldPos.y;

        return t.SampleHeight(worldPos) + t.transform.position.y;
    }
}
