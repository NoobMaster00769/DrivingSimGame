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
    public float boundaryHeight = 2f;
    public float boundaryThickness = 0.5f;

    private Vector3 currentPosition;
    private Vector3 currentForward;

    private float smoothCurvature;
    private float sectionCurvature;
    private float sectionWidth;
    private float sectionBanking;

    private int chunksInCurrentSection;

    private List<GameObject> roadChunks = new List<GameObject>();
    private List<GameObject> waterChunks = new List<GameObject>();
    private List<GameObject> starRoadChunks = new List<GameObject>();
    private List<GameObject> boundaryChunks = new List<GameObject>();

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

        SpawnWaterGrid(currentPosition, roadRotation);
        SpawnStarRoadFX(chunk);
        SpawnBoundaryFX(chunk);

        roadChunks.Add(chunk);

        currentPosition += currentForward * chunkLength;

        smoothCurvature = Mathf.Lerp(
            smoothCurvature,
            sectionCurvature,
            0.05f
        );

        float turn = Mathf.Lerp(-2f, 2f, smoothCurvature);

        currentForward =
            Quaternion.AngleAxis(turn, Vector3.up) * currentForward;

        currentForward.Normalize();

        chunksInCurrentSection++;
    }

    void ApplySectionReactivity(GameObject chunk)
    {
        float width =
            Mathf.Lerp(1.3f, 0.75f, sectionWidth);

        chunk.transform.localScale =
            new Vector3(width, 1f, 1f);

        float bank =
            Mathf.Lerp(-4f, 4f, sectionBanking);

        chunk.transform.Rotate(Vector3.forward, bank, Space.Self);
    }

    // =============================
    // STAR ROAD FX (UNCHANGED)
    // =============================

    void SpawnStarRoadFX(GameObject chunk)
    {
        if (!starRoadPrefab) return;

        GameObject fx = Instantiate(starRoadPrefab);
        fx.transform.parent = chunk.transform;
        fx.transform.localPosition = new Vector3(0f, starRoadOffsetY + 0.15f, 0f);
        fx.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps == null) return;

        var main = ps.main;
        var shape = ps.shape;
        var emission = ps.emission;

        float baseWidth = 10f;
        float scaledWidth = baseWidth * chunk.transform.localScale.x;

        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(
            scaledWidth,
            0.05f,
            chunkLength
        );

        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startSpeed = 0f;
        main.startSize = 0.12f;
        main.startLifetime = 6f;
        main.maxParticles = 20000;

        emission.rateOverTime = 1200f * chunk.transform.localScale.x;

        starRoadChunks.Add(fx);
    }
    // =============================
    // BOUNDARY FX + COLLIDERS (FIXED PROPERLY)
    // =============================

    void SpawnBoundaryFX(GameObject chunk)
    {
        if (!boundaryParticlePrefab) return;

        // This MUST match your star road base width
        float baseWidth = 10f;

        float scaledWidth = baseWidth * chunk.transform.localScale.x;
        float halfWidth = scaledWidth * 0.5f;

        CreateBoundary(chunk, -halfWidth);
        CreateBoundary(chunk, halfWidth);
    }

    void CreateBoundary(GameObject chunk, float xOffset)
    {
        // VISUAL PARTICLES
        GameObject fx = Instantiate(boundaryParticlePrefab);
        fx.transform.parent = chunk.transform;
        fx.transform.localPosition =
            new Vector3(xOffset, boundaryOffsetY, 0f);
        fx.transform.localRotation = Quaternion.identity;

        boundaryChunks.Add(fx);

        // PHYSICAL COLLIDER WALL
        GameObject wall = new GameObject("BoundaryCollider");
        wall.transform.parent = chunk.transform;
        wall.transform.localPosition =
            new Vector3(xOffset, boundaryHeight * 0.5f, 0f);
        wall.transform.localRotation = Quaternion.identity;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = new Vector3(
            boundaryThickness,
            boundaryHeight,
            chunkLength
        );

        boundaryChunks.Add(wall);
    }



    // =============================
    // WATER GRID (UNCHANGED)
    // =============================

    void SpawnWaterGrid(Vector3 basePosition, Quaternion roadRotation)
    {
        if (!waterPrefab) return;

        Quaternion waterRotation =
            Quaternion.LookRotation(currentForward, Vector3.up);

        Vector3 centerPos =
            basePosition + Vector3.up * waterOffsetY;

        SpawnWaterSlab(centerPos, waterRotation);

        Vector3 rightDir = waterRotation * Vector3.right;

        for (int i = 1; i <= sideWaterCount; i++)
        {
            Vector3 offset = rightDir * waterWidth * i;

            SpawnWaterSlab(centerPos + offset, waterRotation);
            SpawnWaterSlab(centerPos - offset, waterRotation);
        }
    }

    void SpawnWaterSlab(Vector3 position, Quaternion rotation)
    {
        GameObject water =
            Instantiate(waterPrefab, position, rotation);

        water.transform.localScale =
            new Vector3(waterWidth, 1f, chunkLength);

        waterChunks.Add(water);
    }

    // =============================
    // CLEANUP
    // =============================

    void Cleanup()
    {
        if (roadChunks.Count <= chunksAhead + chunksBehind)
            return;

        GameObject oldestRoad = roadChunks[0];

        float distance =
            Vector3.Distance(player.position, oldestRoad.transform.position);

        if (distance > chunkLength * chunksBehind)
        {
            Destroy(oldestRoad);
            roadChunks.RemoveAt(0);

            int slabsPerChunk = 1 + (sideWaterCount * 2);

            for (int i = 0; i < slabsPerChunk; i++)
            {
                if (waterChunks.Count > 0)
                {
                    Destroy(waterChunks[0]);
                    waterChunks.RemoveAt(0);
                }
            }

            if (starRoadChunks.Count > 0)
            {
                Destroy(starRoadChunks[0]);
                starRoadChunks.RemoveAt(0);
            }

            // 2 particles + 2 colliders per chunk
            for (int i = 0; i < 4; i++)
            {
                if (boundaryChunks.Count > 0)
                {
                    Destroy(boundaryChunks[0]);
                    boundaryChunks.RemoveAt(0);
                }
            }
        }
    }
}
