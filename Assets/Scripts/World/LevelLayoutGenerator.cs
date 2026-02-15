using System.Collections.Generic;
using UnityEngine;

public class LevelLayoutGenerator : MonoBehaviour
{
    public LevelChunkData firstChunk;
    public RoadState roadState;
    public Transform player;

    [Header("Chunk Settings")]
    public float chunkLength = 40f;
    public int chunksAhead = 20;
    public int chunksBehind = 6;

    [Header("Chunk Overlap")]
    public float chunkOverlap = 2f;

    [Header("Section Settings")]
    public int sectionSize = 18;

    [Header("Water")]
    public GameObject waterPrefab;
    public float waterOffsetY = -3f;
    public float waterWidth = 200f;
    public int sideWaterCount = 2;

    [Header("Star Road FX")]
    public GameObject starRoadPrefab;
    public float starRoadOffsetY = 0.05f;

    [Header("Boundary FX")]
    public GameObject boundaryParticlePrefab;
    public float boundaryOffsetY = 0.2f;
    public float boundaryHeight = 4.5f;
    public float boundaryThickness = 0.5f;
    public float boundaryOutwardPadding = 0.2f;

    private Vector3 currentPosition;
    private Vector3 currentForward;

    private float smoothCurvature;
    private float sectionCurvature;
    private float sectionWidth;
    private float sectionBanking;

    private float turnMomentum;
    private int chunksInCurrentSection;

    private List<GameObject> roadChunks = new();
    private List<GameObject> waterChunks = new();
    private List<GameObject> starRoadChunks = new();
    private List<GameObject> boundaryChunks = new();

    private List<Vector3> roadCenters = new(); // 🔥 NEW

    void Start()
    {
        currentPosition = Vector3.zero;
        currentForward = Vector3.forward;

        BeginNewSection();

        for (int i = 0; i < chunksAhead; i++)
            SpawnNextChunk();
    }

    void Update()
    {
        if (!player) return;

        float distanceToEnd =
            Vector3.Distance(player.position, currentPosition);

        if (distanceToEnd < chunkLength * chunksAhead * 0.6f)
            SpawnNextChunk();

        Cleanup();
    }

    void BeginNewSection()
    {
        sectionCurvature = roadState.curvature;
        sectionWidth = roadState.width;
        sectionBanking = roadState.banking;
        chunksInCurrentSection = 0;
    }

    void SpawnNextChunk()
    {
        if (chunksInCurrentSection >= sectionSize)
            BeginNewSection();

        GameObject prefab = firstChunk.levelChunks[0];

        Quaternion roadRotation =
            Quaternion.LookRotation(currentForward, Vector3.up);

        GameObject chunk =
            Instantiate(prefab, currentPosition, roadRotation);

        ApplySectionReactivity(chunk);

        SpawnWaterGrid(chunk.transform.position, roadRotation);
        SpawnStarRoadFX(chunk);

        roadChunks.Add(chunk);
        roadCenters.Add(chunk.transform.position);

        // 🔥 CONTINUOUS BOUNDARY CREATION
        if (roadCenters.Count >= 2)
        {
            CreateBoundarySegment(
                roadCenters[^2],
                roadCenters[^1],
                chunk.transform.localScale.x
            );
        }

        Transform end = chunk.transform.Find("End");

        if (end != null)
        {
            currentPosition =
                end.position - currentForward * chunkOverlap;

            currentForward = end.forward;
        }
        else
        {
            currentPosition += currentForward * (chunkLength - chunkOverlap);
        }

        smoothCurvature = Mathf.Lerp(
            smoothCurvature,
            sectionCurvature,
            0.1f
        );

        float targetTurn =
            Mathf.Lerp(-4.5f, 4.5f, smoothCurvature);

        turnMomentum = Mathf.Lerp(
            turnMomentum,
            targetTurn,
            0.2f
        );

        currentForward =
            Quaternion.AngleAxis(turnMomentum, Vector3.up) * currentForward;

        currentForward.Normalize();

        chunksInCurrentSection++;
    }

    void ApplySectionReactivity(GameObject chunk)
    {
        float width =
            Mathf.Lerp(1.2f, 0.85f, sectionWidth);

        chunk.transform.localScale =
            new Vector3(width, 1f, 1f);

        float bank =
            Mathf.Lerp(-5f, 5f, sectionBanking);

        chunk.transform.Rotate(Vector3.forward, bank, Space.Self);
    }

    // =========================================
    // 🔥 TRUE CONTINUOUS CURVED BOUNDARY SYSTEM
    // =========================================

    void CreateBoundarySegment(Vector3 start, Vector3 end, float widthScale)
    {
        Vector3 dir = (end - start).normalized;
        float length = Vector3.Distance(start, end);

        Vector3 mid = (start + end) * 0.5f;

        float halfRoadWidth = widthScale * 5f; // base mesh width assumed 10

        CreateOneSide(mid, dir, length, -1f, halfRoadWidth);
        CreateOneSide(mid, dir, length, 1f, halfRoadWidth);
    }

    void CreateOneSide(
        Vector3 mid,
        Vector3 forward,
        float length,
        float sideSign,
        float halfWidth)
    {
        Vector3 right =
            Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 pos =
            mid +
            right * sideSign *
            (halfWidth + boundaryOutwardPadding) +
            Vector3.up *
            (boundaryHeight * 0.5f + boundaryOffsetY);

        Quaternion rot =
            Quaternion.LookRotation(forward, Vector3.up);

        // COLLIDER
        GameObject wall = new("BoundaryCollider");
        wall.transform.position = pos;
        wall.transform.rotation = rot;

        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = new Vector3(
            boundaryThickness,
            boundaryHeight,
            length + chunkOverlap
        );

        boundaryChunks.Add(wall);

        // PARTICLE VISUAL
        if (boundaryParticlePrefab)
        {
            GameObject fx =
                Instantiate(boundaryParticlePrefab,
                            pos,
                            rot);

            ParticleSystem ps =
                fx.GetComponent<ParticleSystem>();

            if (ps)
            {
                var shape = ps.shape;
                shape.shapeType =
                    ParticleSystemShapeType.Box;

                shape.scale =
                    new Vector3(
                        boundaryThickness,
                        boundaryHeight,
                        length + chunkOverlap
                    );
            }

            boundaryChunks.Add(fx);
        }
    }

    // =========================================
    // STAR ROAD (UNCHANGED)
    // =========================================

    void SpawnStarRoadFX(GameObject chunk)
    {
        if (!starRoadPrefab) return;

        GameObject fx = Instantiate(starRoadPrefab);
        fx.transform.parent = chunk.transform;
        fx.transform.localPosition =
            new Vector3(0f, starRoadOffsetY + 0.15f, 0f);
        fx.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps == null) return;

        var main = ps.main;
        var shape = ps.shape;
        var emission = ps.emission;

        float baseWidth = 10f;
        float scaledWidth =
            baseWidth * chunk.transform.localScale.x;

        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(
            scaledWidth,
            0.05f,
            chunkLength + chunkOverlap
        );

        main.simulationSpace =
            ParticleSystemSimulationSpace.Local;
        main.startSpeed = 0f;
        main.startSize = 0.12f;
        main.startLifetime = 8f;
        main.maxParticles = 25000;

        emission.rateOverTime =
            1500f * chunk.transform.localScale.x;

        starRoadChunks.Add(fx);
    }

    // =========================================
    // WATER (UNCHANGED)
    // =========================================

    void SpawnWaterGrid(Vector3 basePosition,
                        Quaternion roadRotation)
    {
        if (!waterPrefab) return;

        Quaternion waterRotation =
            Quaternion.LookRotation(currentForward,
                                    Vector3.up);

        Vector3 centerPos =
            basePosition + Vector3.up * waterOffsetY;

        SpawnWaterSlab(centerPos, waterRotation);

        Vector3 rightDir =
            waterRotation * Vector3.right;

        for (int i = 1; i <= sideWaterCount; i++)
        {
            Vector3 offset =
                rightDir * waterWidth * i;

            SpawnWaterSlab(centerPos + offset,
                           waterRotation);

            SpawnWaterSlab(centerPos - offset,
                           waterRotation);
        }
    }

    void SpawnWaterSlab(Vector3 position,
                        Quaternion rotation)
    {
        GameObject water =
            Instantiate(waterPrefab,
                        position,
                        rotation);

        water.transform.localScale =
            new Vector3(waterWidth, 1f, chunkLength);

        waterChunks.Add(water);
    }

    void Cleanup()
    {
        if (roadChunks.Count <=
            chunksAhead + chunksBehind)
            return;

        GameObject oldestRoad = roadChunks[0];

        float distance =
            Vector3.Distance(player.position,
                             oldestRoad.transform.position);

        if (distance > chunkLength * chunksBehind)
        {
            Destroy(oldestRoad);
            roadChunks.RemoveAt(0);
        }
    }
}
