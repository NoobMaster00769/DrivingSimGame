using UnityEngine;
using System.Collections.Generic;

public class RoadSpawner : MonoBehaviour
{
    public RoadState roadState;

    [Header("References")]
    public Transform car;
    public RoadSegment roadPrefab;

    [Header("Generation")]
    public int segmentsAhead = 8;
    public float spawnAheadDistance = 50f;
    public float despawnBehindDistance = 40f;

    [Header("Curvature – Limits")]
    public float maxTurnAngle = 14f;

    [Header("FLOW (always on)")]
    public float flowStrength = 0.4f;
    public float flowFrequency = 0.08f;

    [Header("AGGRESSION RESPONSE")]
    public float aggressionTurnBoost = 1.2f;

    [Header("MEMORY / INTENT")]
    public float intentChangeChance = 0.15f;
    public int minIntentDuration = 4;
    public int maxIntentDuration = 10;

    [Header("Smoothing")]
    public float turnSmoothSpeed = 0.35f;
    public float bankSmoothSpeed = 0.25f;
    public float widthSmoothSpeed = 0.3f;

    [Header("Width")]
    public float baseWidth = 6f;
    public float maxWidthVariation = 1.5f;

    [Header("Banking")]
    public float maxBankAngle = 8f;

    Queue<RoadSegment> spawned = new();
    RoadSegment lastSegment;

    // --- STATE ---
    float currentHeading;
    float currentTurn;
    float currentTurnTarget;

    float currentWidth;
    float widthTarget;

    float currentBank;
    float bankTarget;

    // Intent memory
    int intentTimer;
    float intentDirection; // -1 = left, +1 = right

    float flowTime;

    void Start()
    {
        SpawnInitial();
    }

    void Update()
    {
        if (!car || !lastSegment) return;

        HandleSpawning();
        HandleDespawning();
    }

    // ===================== SPAWNING =====================
    void HandleSpawning()
    {
        Vector3 toEnd = lastSegment.End.position - car.position;
        float forwardDistance = Vector3.Dot(car.forward, toEnd);

        if (forwardDistance < spawnAheadDistance)
            SpawnNext();
    }

    void HandleDespawning()
    {
        if (spawned.Count == 0) return;

        RoadSegment first = spawned.Peek();

        float behindDistance =
            Vector3.Dot(first.End.forward, car.position - first.End.position);

        if (behindDistance > despawnBehindDistance)
            Destroy(spawned.Dequeue().gameObject);
    }

    // ===================== INITIAL =====================
    void SpawnInitial()
    {
        lastSegment = Instantiate(roadPrefab, Vector3.zero, Quaternion.identity);
        spawned.Enqueue(lastSegment);

        currentHeading = 0f;
        currentTurn = 0f;
        currentTurnTarget = 0f;

        currentWidth = baseWidth;
        widthTarget = baseWidth;

        currentBank = 0f;
        bankTarget = 0f;

        intentTimer = 0;
        intentDirection = Random.value < 0.5f ? -1f : 1f;

        for (int i = 0; i < segmentsAhead; i++)
            SpawnNext();
    }

    // ===================== CORE LOGIC =====================
    void SpawnNext()
    {
        RoadSegment next = Instantiate(roadPrefab);

        // ---------- FLOW COMPONENT ----------
        flowTime += flowFrequency;
        float flow =
            (Mathf.PerlinNoise(flowTime, 0f) - 0.5f) *
            maxTurnAngle *
            flowStrength;

        // ---------- MEMORY / INTENT ----------
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

        // ---------- AGGRESSION RESPONSE ----------
        float aggressionCurve = 0f;
        if (roadState)
        {
            aggressionCurve =
                roadState.curvature *
                aggressionTurnBoost *
                maxTurnAngle *
                0.6f;
        }

        // ---------- FINAL TURN TARGET ----------
        currentTurnTarget =
            flow +
            intentCurve +
            aggressionCurve;

        currentTurnTarget = Mathf.Clamp(
            currentTurnTarget,
            -maxTurnAngle,
            maxTurnAngle
        );

        currentTurn = Mathf.Lerp(
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

        currentWidth = Mathf.Lerp(
            currentWidth,
            widthTarget,
            widthSmoothSpeed
        );

        // ---------- BANKING ----------
        bankTarget =
            -currentTurn * 0.6f;

        currentBank = Mathf.Lerp(
            currentBank,
            bankTarget,
            bankSmoothSpeed
        );

        // ---------- APPLY TRANSFORM ----------
        next.transform.rotation =
            Quaternion.Euler(currentBank, currentHeading, 0f);

        Vector3 offset =
            lastSegment.End.position -
            next.Start.position;

        next.transform.position += offset;

        next.transform.localScale = new Vector3(
            currentWidth / baseWidth,
            1f,
            1f
        );

        spawned.Enqueue(next);
        lastSegment = next;

        // ---------- DEBUG ----------
        Debug.Log(
            $"[Road] flow={flow:F1} intent={intentCurve:F1} " +
            $"aggr={aggressionCurve:F1} turn={currentTurn:F1} " +
            $"width={currentWidth:F2}"
        );
    }
}
