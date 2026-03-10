using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
public class LevelLayoutGenerator : MonoBehaviour
{
    public LevelChunkData firstChunk;
    public RoadState roadState;
    public WorldEventDirector director;
    public Transform player;

    [Header("Star Road Depth FX")]
    public GameObject starRoadDepthPrefab;
    public float starDepthOffsetY = 0.35f;

    [Header("CURVATURE STRENGTH")]
    [Range(1f, 30f)] public float yawStrength = 18f;

    [Header("Chunk Settings")]
    public float chunkLength = 40f;
    public int chunksAhead = 20;
    public int chunksBehind = 6;

    [Header("Chunk Overlap")]
    public float chunkOverlap = 2f;

    [Header("Water")]
    public GameObject waterPrefab;
    public float waterOffsetY = -3f;
    public float waterWidth = 200f;
    public int sideWaterCount = 2;

    [Header("Star Road FX")]
    public GameObject starRoadPrefab;
    public float starRoadOffsetY = 0.05f;
    public float starWidthBleed = 1.15f;
    public float starLengthBleed = 1.1f;

    [Header("FX Enhancement")]
    public float fxBrightnessMultiplier = 1.6f;
    public float fxEmissionMultiplier = 2.0f;
    public float skyTintBlend = 0.55f;

    [Header("Boundary FX")]
    public GameObject boundaryParticlePrefab;
    public float boundaryOffsetY = 0.2f;
    public float boundaryHeight = 4.5f;
    public float boundaryThickness = 0.5f;
    public float boundaryOutwardPadding = 0.2f;

    [Header("Collider Seam Fix")]
    public float colliderWidthPadding = 0.4f;

    Vector3 currentPosition;
    Vector3 currentForward;

    float smoothCurvature;
    float turnMomentum;
    float accumulatedYaw;

    List<GameObject> roadChunks = new();
    List<Vector3> roadCenters = new();

    void Start()
    {
        currentPosition = Vector3.zero;
        currentForward = Vector3.forward;
        accumulatedYaw = 0f;

        for (int i = 0; i < chunksAhead; i++)
            SpawnNextChunk();
    }

    void Update()
    {
        if (!player) return;

        float distanceToEnd =
            Vector3.Distance(player.position, currentPosition);

        // 🔥 Dynamic lookahead based on curvature intensity
        int dynamicChunksAhead = chunksAhead;

        if (roadState != null && roadState.tempest > 0.6f)
        {
            // During spiral/high intensity reduce prebuild distance
            dynamicChunksAhead = Mathf.Max(6, chunksAhead / 2);
        }

        if (distanceToEnd < chunkLength * dynamicChunksAhead * 0.6f)
            SpawnNextChunk();


        Cleanup();
    }

    void SpawnNextChunk()
    {
        currentForward =
            Quaternion.Euler(0f, accumulatedYaw, 0f) *
            Vector3.forward;

        Quaternion roadRotation =
            Quaternion.LookRotation(currentForward, Vector3.up);

        GameObject prefab = firstChunk.levelChunks[0];
        GameObject chunk =
            Instantiate(prefab, currentPosition, roadRotation);

        ApplySectionReactivity(chunk);
        SpawnWaterGrid(chunk.transform.position);
        SpawnStarRoadFX(chunk);

        BoxCollider roadCol = chunk.GetComponent<BoxCollider>();
        if (roadCol != null)
        {
            roadCol.size = new Vector3(
                roadCol.size.x + colliderWidthPadding,
                roadCol.size.y,
                roadCol.size.z);
        }

        roadChunks.Add(chunk);
        roadCenters.Add(chunk.transform.position);

        if (roadCenters.Count >= 2)
        {
            CreateBoundarySegment(
                roadCenters[^2],
                roadCenters[^1],
                chunk.transform.localScale.x,
                chunk.transform);
        }

        Transform end = chunk.transform.Find("End");

        if (end != null)
            currentPosition =
                end.position - currentForward * chunkOverlap;
        else
            currentPosition +=
                currentForward * (chunkLength - chunkOverlap);
        // responsive but not damped
        smoothCurvature =
            Mathf.Lerp(smoothCurvature, roadState.curvature, 0.45f);

        float targetTurn = smoothCurvature * yawStrength;

        // spring-like turn momentum (no energy loss)
        turnMomentum += (targetTurn - turnMomentum) * 0.5f;

        accumulatedYaw += turnMomentum;

    }

    void ApplySectionReactivity(GameObject chunk)
    {
        float width =
            Mathf.Lerp(1.2f, 0.85f, roadState.width);

        chunk.transform.localScale =
            new Vector3(width, 1f, 1f);

        float bank =
            Mathf.Lerp(-5f, 5f, roadState.banking);

        chunk.transform.Rotate(Vector3.forward, bank, Space.Self);
    }

    // =========================
    // STAR FX ENHANCED
    // =========================
    void SpawnStarRoadFX(GameObject chunk)
    {
        if (!starRoadPrefab) return;

        GameObject fx = Instantiate(starRoadPrefab);
        fx.transform.parent = chunk.transform;
        fx.transform.localPosition =
            new Vector3(0f, starRoadOffsetY + 0.15f, 0f);
        fx.transform.localRotation = Quaternion.identity;

        EnhanceFX(
            fx,
            chunk.transform.localScale.x,
            chunkLength + chunkOverlap,
            true
        );
        
        var fade = fx.AddComponent<ChunkDistanceFade>();
        fade.player = player;
        fade.maxDistance = chunkLength * 5f;
        // =========================
        // COSMIC DEPTH LAYER
        // =========================
        if (starRoadDepthPrefab)
        {
            GameObject depth =
                Instantiate(starRoadDepthPrefab);

            depth.transform.parent = chunk.transform;

            depth.transform.localPosition =
                new Vector3(0f, starDepthOffsetY, 0f);

            depth.transform.localRotation = Quaternion.identity;

            EnhanceFX(
                depth,
                chunk.transform.localScale.x,
                chunkLength + chunkOverlap,
                true
            );

            var fade2 = depth.AddComponent<ChunkDistanceFade>();
            fade2.player = player;
            fade2.maxDistance = chunkLength * 6f;
        }
    }


    // =========================
    // BOUNDARY FX ENHANCED
    // =========================
    void CreateBoundarySegment(Vector3 start, Vector3 end, float widthScale, Transform parent)

    {
        Vector3 dir = (end - start).normalized;
        float length = Vector3.Distance(start, end);
        Vector3 mid = (start + end) * 0.5f;

        float halfRoadWidth = widthScale * 5f;

        CreateOneSide(mid, dir, length, -1f, halfRoadWidth, parent);
        CreateOneSide(mid, dir, length, 1f, halfRoadWidth, parent);

    }

    void CreateOneSide(Vector3 mid, Vector3 forward,
                    float length, float sideSign,
                    float halfWidth,
                    Transform parent)

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

        GameObject wall = new("BoundaryCollider");
        wall.transform.parent = parent;
        wall.transform.position = pos;
        wall.transform.rotation = rot;
        
        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = new Vector3(
            boundaryThickness,
            boundaryHeight,
            length + chunkOverlap);

        if (boundaryParticlePrefab)
        {
            GameObject fx =
                Instantiate(boundaryParticlePrefab,
                            pos,
                            rot);

            fx.transform.parent = parent; // 🔥 THIS IS THE FIX

            EnhanceFX(fx, 1f, length + chunkOverlap, false);

            var fade = fx.AddComponent<ChunkDistanceFade>();
            fade.player = player;
            fade.maxDistance = chunkLength * 5f;
        }

    }

    // =========================
    // UNIVERSAL FX ENHANCER
    // =========================
    void EnhanceFX(GameObject fx, float widthScale,
                float length, bool isStar)
    {
        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps == null) return;

        var shape = ps.shape;
        var emission = ps.emission;
        var main = ps.main;

        // -----------------------------
        // SHAPE
        // -----------------------------
        if (isStar)
        {
            float baseWidth = 10f;
            float scaledWidth = baseWidth * widthScale;

            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(
                scaledWidth * starWidthBleed,
                0.05f,
                length * starLengthBleed);
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(
                boundaryThickness,
                boundaryHeight,
                length);
        }

        // -----------------------------
        // EMISSION DENSITY
        // -----------------------------
        var rate = emission.rateOverTime;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(
            rate.constant * fxEmissionMultiplier);

        // -----------------------------
        // SIZE VARIATION (brightness depth)
        // -----------------------------
        float baseSize = main.startSize.constant;
        main.startSize = new ParticleSystem.MinMaxCurve(
            baseSize * 1.0f,
            baseSize * 1.35f
        );

        // -----------------------------
        // COSMIC COLOR WITH VARIATION
        // -----------------------------
        Color coolWhite = new Color(0.85f, 0.92f, 1f);
        Color softCyan = new Color(0.6f, 0.85f, 1f);
        Color softBlue = new Color(0.5f, 0.7f, 1f);

        // Blend base cosmic palette
        Color midTone = Color.Lerp(coolWhite, softCyan, 0.5f);

        // Slight sky influence
        if (RenderSettings.skybox != null)
        {
            Color skyTint = RenderSettings.skybox.GetColor("_Tint");
            midTone = Color.Lerp(midTone, skyTint, 0.25f);
        }

        // Controlled HDR boost
        midTone *= 1.1f;

        Color minColor = Color.Lerp(midTone, softBlue, 0.3f);
        Color maxColor = Color.Lerp(midTone, coolWhite, 0.2f);

        main.startColor = new ParticleSystem.MinMaxGradient(minColor, maxColor);

        // -----------------------------
        // FADE-IN (smooth and natural)
        // -----------------------------
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient grad = new Gradient();

        grad.SetKeys(
            new GradientColorKey[]
            {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
            new GradientAlphaKey(0f, 0f),
            new GradientAlphaKey(0.85f, 0.25f),
            new GradientAlphaKey(1f, 1f)
            }
        );

        colorOverLifetime.color =
            new ParticleSystem.MinMaxGradient(grad);
    }

    void SpawnWaterGrid(Vector3 basePosition)
    {
        if (!waterPrefab) return;

        Quaternion waterRotation =
            Quaternion.LookRotation(currentForward, Vector3.up);

        Vector3 centerPos =
            basePosition + Vector3.up * waterOffsetY;

        SpawnWaterSlab(centerPos, waterRotation);

        Vector3 rightDir =
            waterRotation * Vector3.right;

        for (int i = 1; i <= sideWaterCount; i++)
        {
            Vector3 offset =
                rightDir * waterWidth * i;

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
    }

    void Cleanup()
    {
        if (roadChunks.Count <= chunksAhead + chunksBehind)
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
