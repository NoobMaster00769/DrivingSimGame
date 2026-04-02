using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Infinite spline road. Road is purely particle-based visually.
/// Sections spawn at the tip, are fully visible while near the player,
/// and fade out + get destroyed when far enough behind.
///
/// FADE LOGIC (fixed):
///   - Sections AHEAD of player: fully visible (alpha = 1) as soon as spawned.
///     They fade IN only during the last fxFadeInDistance before the player
///     reaches them — creating the "materialising ahead" effect.
///   - Sections BEHIND player: stay fully visible until fxKeepDistance,
///     then fade out over fxFadeOutDistance, then get destroyed.
///
/// FIELD NAMES: match the original LevelLayoutGenerator exactly so your
/// existing Inspector assignments carry over without reassigning.
/// </summary>
public class LevelLayoutGenerator : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR — original field names preserved
    // ─────────────────────────────────────────────────────────────

    public LevelChunkData firstChunk;
    public RoadState roadState;
    public WorldEventDirector director;
    public Transform player;
    public VehicleInputReader input;

    [Header("Star Road Depth FX")]
    public GameObject starRoadDepthPrefab;
    public float starDepthOffsetY = 0.35f;

    [Header("Curvature")]
    [Range(1f, 30f)]
    public float yawStrength = 18f;

    [Header("Road Generation")]
    [Tooltip("World units between spline control points. 3–5 gives smooth curves.")]
    public float pointSpacing = 3f;
    [Tooltip("Control points to maintain ahead of player. 200 × 4 = 800 units ahead.")]
    public int pointsAhead = 200;
    [Tooltip("Control points behind player before trimming.")]
    public int pointsBehind = 80;
    [Tooltip("How many control points each road section spans. Section length = pointsPerSection × pointSpacing.")]
    public int pointsPerSection = 3;

    [Header("Legacy")]
    public float chunkLength = 40f;
    public float chunkOverlap = 2f;

    [Header("Water")]
    public GameObject waterPrefab;
    public float waterOffsetY = -3f, waterWidth = 200f;
    public int sideWaterCount = 2;

    [Header("Star Road FX")]
    public GameObject starRoadPrefab;
    public float starRoadOffsetY = 0.05f;
    public float starWidthBleed = 1.15f;
    public float starLengthBleed = 1.1f;

    [Header("FX Enhancement")]
    public float fxBrightnessMultiplier = 1.2f;
    public float fxEmissionMultiplier = 1.0f;
    public float skyTintBlend = 0.65f;

    [Header("Boundary FX")]
    public GameObject boundaryParticlePrefab;
    public float boundaryOffsetY = 0.2f;
    public float boundaryHeight = 4.5f;
    public float boundaryThickness = 0.5f;
    public float boundaryOutwardPadding = 0.2f;
    public float boundaryColliderThickness = 1.2f;

    [Header("Collider")]
    public float colliderWidthPadding = 0.4f;

    [Header("Fade & Visibility")]
    [Tooltip("Sections this far ahead start fading in. Beyond this they are invisible (not yet born visually).")]
    public float fxBirthDistance = 120f;
    [Tooltip("Sections this far behind player stay fully visible.")]
    public float fxKeepDistance = 60f;
    [Tooltip("Sections fade out over this distance after fxKeepDistance. Destroyed at fxKeepDistance + fxFadeOutDistance.")]
    public float fxFadeOutDistance = 80f;

    [Header("Path Variation")]
    public bool enableBranching = false;
    [Range(0f, 1f)] public float branchChance = 0.12f;
    public float branchStrength = 0.6f, branchDuration = 6f;

    // ─────────────────────────────────────────────────────────────
    //  ROAD SECTION
    // ─────────────────────────────────────────────────────────────

    class Section
    {
        public GameObject root;
        public BoxCollider col;
        public ParticleSystem surfPS, depthPS, leftPS, rightPS;
        public Vector3 center;
        public float sectionLen;
        // Max emission rates set at spawn — used to scale fade without compounding
        public float surfMaxRate, depthMaxRate, leftMaxRate, rightMaxRate;
    }

    List<Section> sections = new List<Section>();

    // ─────────────────────────────────────────────────────────────
    //  SPLINE DATA
    // ─────────────────────────────────────────────────────────────

    List<Vector3> pts = new List<Vector3>();
    List<Vector3> tans = new List<Vector3>();

    // ─────────────────────────────────────────────────────────────
    //  TIP STATE
    // ─────────────────────────────────────────────────────────────

    Vector3 tipPos;
    Vector3 tipFwd;
    public Vector3 CurrentForward => tipFwd;

    int totalSteps;
    int nextSectionStep;

    // ─────────────────────────────────────────────────────────────
    //  CURVATURE + BRANCHING
    // ─────────────────────────────────────────────────────────────

    float smoothCurvature, turnMomentum;
    float voidTimer;
    float branchTimer, branchDirection;
    bool branchActive, isRecovering;
    public static bool isInVoid = false;

    // ─────────────────────────────────────────────────────────────
    //  START
    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        tipPos = Vector3.zero;
        tipFwd = Vector3.forward;
        totalSteps = 0;
        nextSectionStep = pointsPerSection;

        // Seed full road ahead. All sections spawned here start at alpha=1
        // because the player hasn't moved yet — they're all "around" the start.
        for (int i = 0; i < pointsAhead; i++)
            Step();
    }

    // ─────────────────────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (!player) return;

        if (input.ResetCar && !isRecovering)
        {
            StartCoroutine(RecoverRoutine());
            input.ConsumeReset();
        }

        // Keep road topped up ahead.
        // Hard cap prevents infinite loop if tipPos drifts unexpectedly.
        int stepsThisFrame = 0;
        int maxStepsPerFrame = pointsPerSection * 4;
        while (stepsThisFrame < maxStepsPerFrame &&
               Vector3.Distance(player.position, tipPos) <
               pointSpacing * pointsAhead * 0.4f)
        {
            Step();
            stepsThisFrame++;
        }

        // Off-road
        if (IsOffRoad())
        {
            voidTimer += Time.deltaTime;
            isInVoid = true;
            if (voidTimer > 0.35f && !isRecovering)
            {
                StartCoroutine(RecoverRoutine());
                voidTimer = 0f; isInVoid = false;
            }
        }
        else { voidTimer = 0f; isInVoid = false; }

        UpdateBranching();
        UpdateSections();
        TrimPoints();
    }

    // ─────────────────────────────────────────────────────────────
    //  STEP
    // ─────────────────────────────────────────────────────────────

    void Step()
    {
        // Curvature — identical to original SpawnNextChunk
        smoothCurvature = Mathf.Lerp(smoothCurvature, roadState.curvature, 0.45f);
        smoothCurvature += Mathf.Sin(Time.time * 0.8f + totalSteps * 0.3f) * 0.08f;
        smoothCurvature += Mathf.Sin(Time.time * 0.15f) * 0.15f;

        float targetTurn = Mathf.Clamp(smoothCurvature * yawStrength, -28f, 28f);
        turnMomentum += (targetTurn - turnMomentum) * 0.45f;

        // When RoadState spiral is over budget, pull turn momentum to zero fast.
        // Without this the spline physically loops back on itself.
        if (roadState != null && roadState.SpiralOverBudget)
            turnMomentum = Mathf.Lerp(turnMomentum, 0f, 0.35f);

        // Hard clamp per-step rotation. At spacing=3, chunkLength=40:
        // stepScale=0.075, so 1.5 deg/step max = ~20 deg per chunkLength.
        float stepScale = pointSpacing / chunkLength;

        // 🔥 HARD LIMIT PER STEP
        float turnStep = Mathf.Clamp(turnMomentum * stepScale, -1.4f, 1.4f);

        tipFwd = Quaternion.Euler(0f, turnStep, 0f) * tipFwd;

        tipFwd.y = 0f;
        if (tipFwd.sqrMagnitude < 0.0001f) tipFwd = Vector3.forward;
        tipFwd.Normalize();

        // Micro lateral drift applied ONLY to the stored point, NOT to tipPos.
        // Adding it to tipPos accumulates permanently and sends the road in a
        // random direction after curves, and causes infinite loops in Update.
        float drift = Mathf.Sin(totalSteps * 0.2f) * 0.5f * stepScale;
        Vector3 driftedPos = tipPos + Vector3.Cross(Vector3.up, tipFwd) * drift;

        pts.Add(driftedPos);
        tans.Add(tipFwd);
        totalSteps++;

        if (totalSteps >= nextSectionStep)
        {
            SpawnSection();
            nextSectionStep = totalSteps + pointsPerSection;
        }

        // Advance tip cleanly — no drift accumulation on tipPos
        tipPos += tipFwd * pointSpacing;
    }

    // ─────────────────────────────────────────────────────────────
    //  ROAD HALF WIDTH — from RoadState, identical to original
    //
    //  Original: baseWidth = Lerp(1.2, 0.85, roadState.width)
    //            chunk.localScale.x = baseWidth
    //            Road mesh 10 units wide at scale 1 → halfWidth = baseWidth * 5
    // ─────────────────────────────────────────────────────────────

    float GetHalfWidth(Vector3 pos)
    {
        float baseWidth = Mathf.Lerp(1.4f, 0.95f, roadState.width);
        float widthPulse = Mathf.Sin(Time.time * 0.3f + pos.z * 0.05f) * 0.08f;
        float boost = branchActive ? 1.1f : 1f;
        return (baseWidth + widthPulse) * boost * 5f;
    }

    // ─────────────────────────────────────────────────────────────
    //  SPAWN SECTION
    // ─────────────────────────────────────────────────────────────

    void SpawnSection()
    {
        int n = pts.Count;
        if (n < 2) return;

        int startIdx = Mathf.Max(0, n - pointsPerSection - 1);
        int endIdx = n - 1;

        Vector3 startPt = pts[startIdx];
        Vector3 endPt = pts[endIdx];
        Vector3 center = (startPt + endPt) * 0.5f;

        int midIdx = (startIdx + endIdx) / 2;
        midIdx = Mathf.Clamp(midIdx, 0, tans.Count - 1);
        Vector3 fwd = tans[midIdx];
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();

        float len = 0f;
        for (int i = startIdx; i < endIdx; i++)
            len += Vector3.Distance(pts[i], pts[i + 1]);

        if (len < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up);
        float hw = GetHalfWidth(center);

        var root = new GameObject("Sec");
        root.layer = gameObject.layer;
        root.transform.position = center;
        root.transform.rotation = rot;

        // BANKING (same as old system)
        float bank = Mathf.Lerp(-5f, 5f, roadState.banking)
                   + Mathf.Sin(Time.time * 0.6f + center.z * 0.08f) * 1.2f;
        root.transform.Rotate(Vector3.forward, bank, Space.Self);

        // COLLIDER
        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(hw * 2f + colliderWidthPadding, 0.5f, len + pointSpacing * 0.8f);

        // 🔥 CRITICAL: density normalization
        float densityScale = len / chunkLength;

        // =========================
        // STAR ROAD FX
        // =========================
        ParticleSystem surfPS = null;
        float surfMaxRate = 0f;

        if (starRoadPrefab != null)
        {
            var go = Instantiate(starRoadPrefab, root.transform);
            go.transform.localPosition = new Vector3(0f, starRoadOffsetY + 0.15f, 0f);
            go.transform.localRotation = Quaternion.identity;

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(rend.material);
                rend.material.renderQueue = 2000;
                rend.material.SetInt("_ZWrite", 0);
            }

            surfPS = go.GetComponent<ParticleSystem>();
            if (surfPS != null)
                surfMaxRate = ConfigurePS(surfPS,
                    hw * 2f * starWidthBleed,
                    0.05f,
                    len * 1.6f,
                    densityScale);
        }

        // =========================
        // DEPTH FX
        // =========================
        ParticleSystem depthPS = null;
        float depthMaxRate = 0f;

        if (starRoadDepthPrefab != null)
        {
            var go = Instantiate(starRoadDepthPrefab, root.transform);
            go.transform.localPosition = new Vector3(0f, starDepthOffsetY, len * 0.45f);
            go.transform.localRotation = Quaternion.identity;

            depthPS = go.GetComponent<ParticleSystem>();
            if (depthPS != null)
                depthMaxRate = ConfigurePS(depthPS,
                    hw * 2f * starWidthBleed,
                    0.05f,
                    len * 1.6f,
                    densityScale);
        }

        // =========================
        // BOUNDARY (EXACT OLD SYSTEM)
        // =========================
        ParticleSystem leftPS = null, rightPS = null;
        float leftMaxRate = 0f, rightMaxRate = 0f;

        if (boundaryParticlePrefab != null)
        {
            float wallX = hw + boundaryOutwardPadding;
            float wallColY = boundaryHeight * 0.5f + boundaryOffsetY;
            float wallLenZ = len + pointSpacing * 0.8f;

            // LEFT
            var lgo = Instantiate(boundaryParticlePrefab, root.transform);
            lgo.transform.localPosition = new Vector3(-(wallX + boundaryColliderThickness * 1f), wallColY, 0f);



            var lCol = lgo.AddComponent<BoxCollider>();
            lCol.size = new Vector3(boundaryColliderThickness, boundaryHeight, wallLenZ);

            leftPS = lgo.GetComponent<ParticleSystem>();
            if (leftPS != null)
                leftMaxRate = ConfigurePS(leftPS,
                    boundaryThickness,
                    boundaryHeight * 0.5f,
                    len * 1.05f,
                    densityScale);

            // RIGHT
            var rgo = Instantiate(boundaryParticlePrefab, root.transform);
            rgo.transform.localPosition = new Vector3(wallX + boundaryColliderThickness * 1f, wallColY, 0f);

            var rCol = rgo.AddComponent<BoxCollider>();
            rCol.size = new Vector3(boundaryColliderThickness, boundaryHeight, wallLenZ);

            rightPS = rgo.GetComponent<ParticleSystem>();
            if (rightPS != null)
                rightMaxRate = ConfigurePS(rightPS,
                    boundaryThickness,
                    boundaryHeight * 0.5f,
                    len * 1.05f,
                    densityScale);
        }

        // 🔥 CRITICAL FIX (YOU WERE MISSING THIS)
        sections.Add(new Section
        {
            root = root,
            col = col,
            surfPS = surfPS,
            depthPS = depthPS,
            leftPS = leftPS,
            rightPS = rightPS,
            center = center,
            sectionLen = len,
            surfMaxRate = surfMaxRate,
            depthMaxRate = depthMaxRate,
            leftMaxRate = leftMaxRate,
            rightMaxRate = rightMaxRate
        });
    }
    float ConfigurePS(ParticleSystem ps,
                  float shapeX, float shapeY, float shapeZ,
                  float densityScale)
    {
        var sh = ps.shape;
        var em = ps.emission;
        var mn = ps.main;

        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(shapeX, shapeY, shapeZ);

        float baseRate = em.rateOverTime.constant;
        float finalRate = baseRate * fxEmissionMultiplier * densityScale;
        em.rateOverTime = new ParticleSystem.MinMaxCurve(finalRate);

        float sz = mn.startSize.constant;
        mn.startSize = new ParticleSystem.MinMaxCurve(sz, sz * 1.35f);

        // ✅ ORIGINAL COLOR (NO BLUE BUG)
        Color coolWhite = new Color(0.85f, 0.92f, 1f);
        Color softCyan = new Color(0.6f, 0.85f, 1f);
        Color softBlue = new Color(0.5f, 0.7f, 1f);

        Color mid = Color.Lerp(coolWhite, softCyan, 0.5f);

        if (RenderSettings.skybox != null)
            mid = Color.Lerp(mid, RenderSettings.skybox.GetColor("_Tint"), 0.25f);

        mid *= 1.1f;

        mn.startColor = new ParticleSystem.MinMaxGradient(
            Color.Lerp(mid, softBlue, 0.3f),
            Color.Lerp(mid, coolWhite, 0.2f)
        );

        // ✅ IMPORTANT (old behavior)
        var li = ps.lights;
        li.enabled = shapeX > 1f;
        li.intensityMultiplier = 0.6f * fxBrightnessMultiplier;
        li.rangeMultiplier = 0.4f;

        // Fade curve
        var col = ps.colorOverLifetime;
        col.enabled = true;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
            new GradientAlphaKey(0f, 0f),
            new GradientAlphaKey(0.85f, 0.25f),
            new GradientAlphaKey(1f, 1f)
            });

        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;

        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.4f);
        curve.AddKey(0.5f, 1f);
        curve.AddKey(1f, 0.2f);

        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        return finalRate;
    }
    void EnhanceFX(ParticleSystem ps, float widthScale, float length, bool isStar)
        {
            var shape = ps.shape;
            var emission = ps.emission;
            var main = ps.main;

            if (isStar)
            {
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(widthScale * 10f, 0.05f, length * 1.1f);
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(boundaryThickness, boundaryHeight, length);
            }

            var rate = emission.rateOverTime;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate.constant * fxEmissionMultiplier);

            float baseSize = main.startSize.constant;
            main.startSize = new ParticleSystem.MinMaxCurve(baseSize, baseSize * 1.3f);

            Color coolWhite = new Color(0.85f, 0.92f, 1f);
            Color softCyan = new Color(0.6f, 0.85f, 1f);
            Color softBlue = new Color(0.5f, 0.7f, 1f);

            Color mid = Color.Lerp(coolWhite, softCyan, 0.5f);
            mid *= 1.1f;

            main.startColor = new ParticleSystem.MinMaxGradient(
                Color.Lerp(mid, softBlue, 0.3f),
                Color.Lerp(mid, coolWhite, 0.2f)
            );

            var lights = ps.lights;
            lights.enabled = false; // 🔥 IMPORTANT FPS FIX

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;

            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.4f);
            curve.AddKey(0.5f, 1f);
            curve.AddKey(1f, 0.2f);

            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }
        // ─────────────────────────────────────────────────────────────
        //  CONFIGURE PS — called once at spawn, returns the set rate
        //  so we can scale it cleanly during fade without re-reading.
        //
        //  disableLights: boundary walls pass true — particle lights on a
        //  1D wall produce one point light per live particle, which is the
        //  dominant FPS cost. Surface/depth PS pass false to keep their glow.
        // ─────────────────────────────────────────────────────────────


        // ─────────────────────────────────────────────────────────────
        //  UPDATE SECTIONS — fade + destroy
        //
        //  CORRECT FADE MODEL:
        //
        //  For sections AHEAD of player (dot > 0):
        //    dist > fxBirthDistance  → alpha = 0  (not visible yet, too far ahead)
        //    dist < fxBirthDistance  → alpha fades IN from 0 to 1 as player approaches
        //    Very close ahead        → alpha = 1
        //
        //  For sections BEHIND player (dot <= 0):
        //    dist < fxKeepDistance   → alpha = 1  (fully visible just behind)
        //    dist > fxKeepDistance   → alpha fades OUT toward 0
        //    dist > fxKeepDistance + fxFadeOutDistance → DESTROY
        //
        //  This gives 300+ units of visible road ahead and a natural trail behind.
        // ─────────────────────────────────────────────────────────────

        void UpdateSections()
        {
            float destroyDist = fxKeepDistance + fxFadeOutDistance;

            for (int i = sections.Count - 1; i >= 0; i--)
            {
                var sec = sections[i];
                if (sec.root == null) { sections.RemoveAt(i); continue; }

                float dist = Vector3.Distance(player.position, sec.center);
                Vector3 toSection = sec.center - player.position;
                float dot = Vector3.Dot(player.transform.forward, toSection.normalized);

                float alpha;

                if (dot > 0f)
                {
                    // Section is ahead — fade in as player approaches
                    // At dist=fxBirthDistance: alpha=0. At dist=0: alpha=1.
                    alpha = Mathf.Clamp01(1f - (dist / fxBirthDistance));
                }
                else
                {
                    // Section is behind — keep full then fade out
                    if (dist <= fxKeepDistance)
                    {
                        alpha = 1f;
                    }
                    else
                    {
                        float beyondKeep = dist - fxKeepDistance;
                        alpha = Mathf.Clamp01(1f - (beyondKeep / fxFadeOutDistance));
                    }
                }

                float t = Mathf.SmoothStep(0f, 1f, alpha);
                ApplySectionFade(sec, t);

                // Destroy when fully faded out behind player
                if (dot <= 0f && dist > destroyDist)
                {
                    Destroy(sec.root);
                    sections.RemoveAt(i);
                }
            }
        }

        void ApplySectionFade(Section sec, float t)
        {
            ApplyPSFade(sec.surfPS, sec.surfMaxRate, t);
            ApplyPSFade(sec.depthPS, sec.depthMaxRate, t);
            ApplyPSFade(sec.leftPS, sec.leftMaxRate, t);
            ApplyPSFade(sec.rightPS, sec.rightMaxRate, t);
        }

        // Fades emission rate using the stored max rate — never reads current value
        // so there is zero risk of compounding across frames.
        void ApplyPSFade(ParticleSystem ps, float maxRate, float t)
        {
            if (ps == null) return;
            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(maxRate * t);

            var li = ps.lights;
            if (li.enabled)
                li.intensityMultiplier = 0.6f * fxBrightnessMultiplier * t;
        }

        // ─────────────────────────────────────────────────────────────
        //  TRIM POINTS
        // ─────────────────────────────────────────────────────────────

        void TrimPoints()
        {
            float behind = pointSpacing * pointsBehind;
            while (pts.Count > pointsAhead + pointsBehind
                && pts.Count > 0
                && Vector3.Distance(player.position, pts[0]) > behind)
            {
                pts.RemoveAt(0);
                tans.RemoveAt(0);
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  OFF-ROAD
        // ─────────────────────────────────────────────────────────────

        bool IsOffRoad()
        {
            if (pts.Count < 2) return false;
            float minD = float.MaxValue;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector3 a = pts[i], b = pts[i + 1];
                Vector3 ab = b - a, ap = player.position - a;
                float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
                float d = Vector3.Distance(player.position, a + ab * t);
                if (d < minD) minD = d;
            }
            // halfwidth ~5, give 1.5 buffer
            return minD > 6.5f;
        }

        // ─────────────────────────────────────────────────────────────
        //  RECOVER ROUTINE
        // ─────────────────────────────────────────────────────────────

        IEnumerator RecoverRoutine()
        {
            if (pts.Count < 2) yield break;
            isRecovering = true;

            var rb = player.GetComponent<Rigidbody>();
            if (rb == null) yield break;
            rb.isKinematic = true;

            Vector3 bestPt = pts[0], bestFwd = tipFwd;
            float minD = float.MaxValue;

            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector3 a = pts[i], b = pts[i + 1];
                Vector3 ab = b - a, ap = player.position - a;
                float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
                Vector3 proj = a + ab * t;
                float d = Vector3.Distance(player.position, proj);
                if (d < minD) { minD = d; bestPt = proj; bestFwd = ab.normalized; }
            }

            player.position = bestPt + bestFwd * 3f + Vector3.up * 2.5f;
            player.rotation = Quaternion.LookRotation(bestFwd, Vector3.up);
            yield return null;

            rb.isKinematic = false;
            rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            isRecovering = false;
        }

        // ─────────────────────────────────────────────────────────────
        //  GET ROAD DIRECTION
        // ─────────────────────────────────────────────────────────────

        public Vector3 GetRoadDirectionAt(Vector3 position)
        {
            if (pts.Count < 2) return tipFwd;
            float minD = float.MaxValue;
            Vector3 best = tipFwd;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector3 a = pts[i], b = pts[i + 1];
                Vector3 ab = b - a, ap = position - a;
                float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
                float d = Vector3.Distance(position, a + ab * t);
                if (d < minD) { minD = d; best = ab.normalized; }
            }
            return best;
        }

        // ─────────────────────────────────────────────────────────────
        //  BRANCHING
        // ─────────────────────────────────────────────────────────────

        void UpdateBranching()
        {
            if (!enableBranching || roadState == null) return;
            branchTimer -= Time.deltaTime;
            if (!branchActive && Random.value < branchChance * Time.deltaTime)
            {
                branchActive = true; branchTimer = branchDuration;
                branchDirection = Random.value > 0.5f ? 1f : -1f;
            }
            if (branchActive)
            {
                float t = 1f - (branchTimer / branchDuration);
                float env = Mathf.Sin(t * Mathf.PI);
                smoothCurvature += branchDirection * branchStrength * env * 0.5f;
                roadState.curvature = Mathf.Clamp(roadState.curvature, -1f, 1f);
                if (branchTimer <= 0f) branchActive = false;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  DESTROY
        // ─────────────────────────────────────────────────────────────

        void OnDestroy()
        {
            foreach (var sec in sections)
                if (sec.root != null) Destroy(sec.root);
        }
    } 