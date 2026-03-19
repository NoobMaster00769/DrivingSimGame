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
    public VehicleInputReader input;

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

    [Header("Path Variation")]
    public bool enableBranching = true;

    [Range(0f, 1f)] public float branchChance = 0.12f;
    public float branchStrength = 0.6f;
    public float branchDuration = 6f;

    float branchTimer;
    float branchDirection;
    bool branchActive;

    Vector3 currentPosition;
    Vector3 currentForward;

    float smoothCurvature;
    float turnMomentum;
    float accumulatedYaw;
    Vector3 lastLeftEnd;
    Vector3 lastRightEnd;
    bool hasLastEnds = false;

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

        if (input.ResetCar)
        {
            RecoverCar();
            input.ConsumeReset();
        }

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

        UpdateBranching();
        Cleanup();
    }

    void SpawnNextChunk()
    {
        Vector3 targetForward =
            Quaternion.Euler(0f, accumulatedYaw, 0f) *
            Vector3.forward;

        currentForward =
            Vector3.Slerp(currentForward, targetForward, 0.65f);

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

        float microNoise =
    Mathf.Sin(Time.time * 0.8f +
    roadCenters.Count * 0.3f) * 0.08f;

        smoothCurvature += microNoise;

        float macroWave =
    Mathf.Sin(Time.time * 0.2f) * 0.2f;

        smoothCurvature += macroWave;

        float targetTurn = smoothCurvature * yawStrength;

        // spring-like turn momentum (no energy loss)
        turnMomentum += (targetTurn - turnMomentum) * 0.45f;

        // tiny overshoot = natural motion
        turnMomentum *= 1.02f;

        accumulatedYaw += turnMomentum;

        float lateralShift =
    Mathf.Sin(roadCenters.Count * 0.2f) * 0.5f;

        currentPosition +=
            Vector3.Cross(Vector3.up, currentForward) * lateralShift;

    }

    void ApplySectionReactivity(GameObject chunk)
    {
        float baseWidth =
            Mathf.Lerp(1.2f, 0.85f, roadState.width);

        float widthPulse =
            Mathf.Sin(Time.time * 0.3f +
            chunk.transform.position.z * 0.05f) * 0.08f;

        float width = baseWidth + widthPulse;

        float branchWidthBoost = branchActive ? 1.1f : 1f;

        chunk.transform.localScale =
            new Vector3(width * branchWidthBoost, 1f, 1f);
        float baseBank =
            Mathf.Lerp(-5f, 5f, roadState.banking);

        float bankLag =
            Mathf.Sin(Time.time * 0.6f +
            chunk.transform.position.z * 0.08f) * 1.2f;

        float bank = baseBank + bankLag;

        chunk.transform.Rotate(Vector3.forward, bank, Space.Self);
        float subtleNoise =
    Mathf.Sin(Time.time * 0.5f +
    chunk.transform.position.z * 0.05f) * 0.05f;

        chunk.transform.Rotate(Vector3.up, subtleNoise * 10f, Space.Self);

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

        // ✅ APPLY FLOAT AFTER POSITION IS SET
        float floatOffset =
            Mathf.Sin(Time.time * 1.5f +
            chunk.transform.position.z * 0.1f) * 0.05f;

        fx.transform.localPosition += Vector3.up * floatOffset;

        // ✅ STABLE MATERIAL INSTANCE
        var r = fx.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(r.material); // prevents shared override bugs
            r.material.renderQueue = 2000;
            r.material.SetInt("_ZWrite", 0);
        }
        fx.transform.rotation =
            Quaternion.LookRotation(currentForward, Vector3.up);

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
                new Vector3(0f, starDepthOffsetY, chunkLength * 0.45f);

            depth.transform.rotation =
                Quaternion.LookRotation(currentForward, Vector3.up);

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
        hasLastEnds = true;
    }

    void CreateOneSide(Vector3 mid, Vector3 forward,
                    float length, float sideSign,
                    float halfWidth,
                    Transform parent)

    {
      //  forward = Vector3.Lerp(forward, currentForward, 0.15f);

        Vector3 right =
            Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 basePos =
            mid +
            right * sideSign *
            (halfWidth + boundaryOutwardPadding);

        Vector3 pos;

        if (hasLastEnds)
        {
            Vector3 prevEnd = (sideSign < 0f) ? lastLeftEnd : lastRightEnd;

            // snap start toward previous end → eliminates gap
            Vector3 toPrev = prevEnd - basePos;
            float alignment = Vector3.Dot(forward, toPrev.normalized);

            // 🚫 if previous end is not in front → DON'T BLEND
            if (alignment > 0.2f)
            {
                float blend = Mathf.Lerp(0.2f, 0.6f, 1f - alignment);
                pos = Vector3.Lerp(basePos, prevEnd, blend);
            }
            else
            {
                pos = basePos;
            }
        }
        else
        {
            pos = basePos;
        }
        // 🔒 prevent forward overshoot beyond midpoint
        float maxForward = length * 0.45f;

        Vector3 center = mid;
        Vector3 offset = pos - center;

        float forwardAmount = Vector3.Dot(offset, forward);

        if (forwardAmount > maxForward)
        {
            pos -= forward * (forwardAmount - maxForward);
        }
        // store current end for next segment
        float safeLength = length * 0.48f; // tiny shrink

        Vector3 currentEnd =
            basePos + forward * safeLength;

        if (sideSign < 0f)
            lastLeftEnd = currentEnd;
        else
            lastRightEnd = currentEnd;



        // final height
        pos += Vector3.up * (boundaryHeight * 0.5f + boundaryOffsetY);

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
     length * 0.9f);

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
        var lights = ps.lights;
        lights.enabled = true;
        lights.intensityMultiplier = 0.6f * fxBrightnessMultiplier;
        lights.rangeMultiplier = 0.4f;
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;

        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.4f);
        curve.AddKey(0.5f, 1f);
        curve.AddKey(1f, 0.2f);

        sizeOverLifetime.size =
            new ParticleSystem.MinMaxCurve(1f, curve);
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

    void RecoverCar()
    {
        if (roadCenters.Count == 0) return;

        // 🔍 find closest road point
        Vector3 closest = roadCenters[0];
        float minDist = float.MaxValue;

        foreach (var c in roadCenters)
        {
            float d = Vector3.Distance(player.position, c);
            if (d < minDist)
            {
                minDist = d;
                closest = c;
            }
        }

        // 🧭 find forward direction from nearby segments
        Vector3 forward = currentForward;

        int index = roadCenters.IndexOf(closest);

        if (index < roadCenters.Count - 1)
        {
            forward = (roadCenters[index + 1] - closest).normalized;
        }
        else if (index > 0)
        {
            forward = (closest - roadCenters[index - 1]).normalized;
        }

        Vector3 newPos = closest + Vector3.up * 2f;

        player.position = newPos;
        player.rotation = Quaternion.LookRotation(forward, Vector3.up);

        // 🧼 reset physics
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void SpawnWaterSlab(Vector3 position, Quaternion rotation)
    {
        GameObject water =
            Instantiate(waterPrefab, position, rotation);

        water.transform.localScale =
            new Vector3(waterWidth, 1f, chunkLength);

        // 🔥 push slightly down (you already had this, keep it)
        water.transform.position += Vector3.down * 0.6f;

        // 🔥 FIX: force render behind everything
        var r = water.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(r.material); // avoid shared issues
            r.material.renderQueue = 1990; // BELOW your road (2000)
            r.material.SetInt("_ZWrite", 1); // VERY IMPORTANT
        }
    }
    void UpdateBranching()
    {
        if (!enableBranching || roadState == null) return;

        branchTimer -= Time.deltaTime;

        if (!branchActive && Random.value < branchChance * Time.deltaTime)
        {
            branchActive = true;
            branchTimer = branchDuration;
            branchDirection = Random.value > 0.5f ? 1f : -1f;
        }

        if (branchActive)
        {
            float t = 1f - (branchTimer / branchDuration);
            float envelope = Mathf.Sin(t * Mathf.PI);

            float influence = branchDirection * branchStrength * envelope;

            roadState.curvature += influence * Time.deltaTime;
            roadState.curvature = Mathf.Clamp(roadState.curvature, -1f, 1f);

            if (branchTimer <= 0f)
                branchActive = false;
        }
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
