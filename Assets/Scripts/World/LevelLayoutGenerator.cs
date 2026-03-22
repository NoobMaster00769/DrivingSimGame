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
    public float fxBrightnessMultiplier = 1.8f;
    public float fxEmissionMultiplier = 2.2f;
    public float skyTintBlend = 0.65f;

    [Header("Boundary FX")]
    public GameObject boundaryParticlePrefab;
    public float boundaryOffsetY = 0.2f;
    public float boundaryHeight = 4.5f;
    public float boundaryThickness = 0.5f;
    public float boundaryOutwardPadding = 0.2f;

    [Header("Collider Seam Fix")]
    public float colliderWidthPadding = 0.4f;

    [Header("Path Variation")]
    public bool enableBranching = false;

    [Range(0f, 1f)] public float branchChance = 0.12f;
    public float branchStrength = 0.6f;
    public float branchDuration = 6f;

    public static bool isInVoid = false;
    float voidTimer = 0f;

    float branchTimer;
    float branchDirection;
    bool branchActive;

    Vector3 currentPosition;
    Vector3 currentForward;
    public Vector3 CurrentForward => currentForward;

    float smoothCurvature;
    float turnMomentum;
    float accumulatedYaw;
    Vector3 lastLeftEnd;
    Vector3 lastRightEnd;
    bool hasLastEnds = false;

    List<GameObject> roadChunks = new();
    List<Vector3> roadCenters = new();
    Queue<Vector3> forwardHistory = new Queue<Vector3>();
    int historySize = 4;

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

        if (input.ResetCar && !isRecovering)
        {
            StartCoroutine(RecoverRoutine());
            input.ConsumeReset();
        }

        float distanceToEnd =
            Vector3.Distance(player.position, currentPosition);

        int dynamicChunksAhead = chunksAhead;

        if (roadState != null && roadState.tempest > 0.6f)
        {
            dynamicChunksAhead = Mathf.Max(6, chunksAhead / 2);
        }

        if (distanceToEnd < chunkLength * dynamicChunksAhead * 0.6f)
            SpawnNextChunk();

        // VOID CHECK (keep this)
        if (IsPlayerOffRoad())
        {
            voidTimer += Time.deltaTime;
            isInVoid = true;

            if (voidTimer > 0.35f && !isRecovering)
            {
                StartCoroutine(RecoverRoutine());
                voidTimer = 0f;
                isInVoid = false;
            }
        }
        else
        {
            voidTimer = 0f;
            isInVoid = false;
        }

        UpdateBranching();
        Cleanup();
    }

    Vector3 SmoothForward()
    {
        if (forwardHistory.Count < 4)
            return currentForward;

        var arr = forwardHistory.ToArray();

        Vector3 p0 = arr[0];
        Vector3 p1 = arr[1];
        Vector3 p2 = arr[2];
        Vector3 p3 = arr[3];

        float t = 0.5f;

        Vector3 result =
            0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
            );

        return result.normalized;
    }

    void SpawnNextChunk()
    {
        Vector3 targetForward =
            Quaternion.Euler(0f, accumulatedYaw, 0f) *
            Vector3.forward;

        forwardHistory.Enqueue(targetForward);

        if (forwardHistory.Count > historySize)
            forwardHistory.Dequeue();

        currentForward = SmoothForward();

        Quaternion roadRotation =
            Quaternion.LookRotation(currentForward, Vector3.up);

        GameObject prefab = firstChunk.levelChunks[0];
        GameObject chunk =
            Instantiate(prefab, currentPosition, roadRotation);

        ApplySectionReactivity(chunk);

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
    Mathf.Sin(Time.time * 0.15f) * 0.15f;

        smoothCurvature += macroWave;


        float targetTurn = smoothCurvature * yawStrength;

        // 🔒 prevent extreme turns
        targetTurn = Mathf.Clamp(targetTurn, -35f, 35f);

        // spring-like turn momentum (no energy loss)
        turnMomentum += (targetTurn - turnMomentum) * 0.45f;

        // tiny overshoot = natural motion
        turnMomentum *= 1.0f;

        accumulatedYaw += turnMomentum;
        accumulatedYaw = Mathf.Clamp(accumulatedYaw, -90f, 90f);

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

            float branchOffset = branchDirection * branchStrength * envelope;

            // apply TEMPORARILY instead of modifying base curvature
            smoothCurvature += branchOffset * 0.5f;
            roadState.curvature = Mathf.Clamp(roadState.curvature, -1f, 1f);

            if (branchTimer <= 0f)
                branchActive = false;
        }
    }

    bool isRecovering = false;

    IEnumerator RecoverRoutine()
    {
        if (roadCenters.Count < 2) yield break;

        isRecovering = true;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) yield break;

        // 🧊 HARD FREEZE (this is the missing piece)
        rb.isKinematic = true;

        // 🔍 find best segment
        Vector3 bestPoint = roadCenters[0];
        Vector3 bestForward = currentForward;
        float minDist = float.MaxValue;

        for (int i = 0; i < roadCenters.Count - 1; i++)
        {
            Vector3 a = roadCenters[i];
            Vector3 b = roadCenters[i + 1];

            Vector3 ab = b - a;
            Vector3 ap = player.position - a;

            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
            Vector3 proj = a + ab * t;

            float d = Vector3.Distance(player.position, proj);

            if (d < minDist)
            {
                minDist = d;
                bestPoint = proj;
                bestForward = ab.normalized;
            }
        }


        // 🔥 push slightly forward along road
        Vector3 forwardOffset = bestForward * 3f;

        Vector3 newPos =
            bestPoint +
            forwardOffset +
            Vector3.up * 2.5f;

        player.position = newPos;
        player.rotation = Quaternion.LookRotation(bestForward, Vector3.up);

        // wait 1 frame so physics doesn't fight back
        yield return null;

        // reset physics cleanly
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isRecovering = false;
    }

    bool IsPlayerOffRoad()
    {
        if (roadCenters.Count < 2) return false;

        float minDist = float.MaxValue;

        for (int i = 0; i < roadCenters.Count - 1; i++)
        {
            Vector3 a = roadCenters[i];
            Vector3 b = roadCenters[i + 1];

            Vector3 ab = b - a;
            Vector3 ap = player.position - a;

            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
            Vector3 closest = a + ab * t;

            float dist = Vector3.Distance(player.position, closest);

            if (dist < minDist)
                minDist = dist;
        }

        // 🔥 THIS VALUE IS KEY
        // road half width ~5 → give buffer
        return minDist > 6.5f;
    }

    public Vector3 GetRoadDirectionAt(Vector3 position)
    {
        if (roadCenters.Count < 2)
            return currentForward;

        float minDist = float.MaxValue;
        Vector3 bestDir = currentForward;

        for (int i = 0; i < roadCenters.Count - 1; i++)
        {
            Vector3 a = roadCenters[i];
            Vector3 b = roadCenters[i + 1];

            Vector3 ab = b - a;
            Vector3 ap = position - a;

            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
            Vector3 proj = a + ab * t;

            float d = Vector3.Distance(position, proj);

            if (d < minDist)
            {
                minDist = d;
                bestDir = ab.normalized;
            }
        }

        return bestDir;
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
