using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CosmicUIController : MonoBehaviour
{
    [Header("References")]
    public VehicleContext vehicle;
    public Camera mainCamera;

    [Header("Startup Reveal")]
    public float revealDuration = 2.5f;

    float revealTimer;
    float revealFactor = 0f;

    [Header("Clear Driving UI")]

    public float textHeight = 140f;
    public float textScale = 12f;
    public float textFadeSpeed = 4f;

    public LevelLayoutGenerator layout;

    GameObject wrongText;
    GameObject upshiftText;
    GameObject downshiftText;

    TextMesh wrongMesh;
    TextMesh upMesh;
    TextMesh downMesh;

    float wrongAlpha;
    float upAlpha;
    float downAlpha;

    [Header("Sky Placement")]
    public float skyDistance = 1000f;
    public float skyHeight = 300f;

    [Header("Arc Layout")]
    public int arcStarCount = 80;
    public int arcLayers = 3;
    public float arcRadius = 220f;
    public float arcAngle = 100f;
    public float arcVerticalCurve = 50f;
    public float arcLayerDepthSpacing = 40f;

    [Header("Arc Positioning (Sky Illusion)")]
    public float arcHorizontalOffset = -150f;
    public float arcVerticalOffset = 60f;
    public float arcScaleMultiplier = 0.75f;

    [Header("Arc Speed Behaviour")]
    public float baseArcThickness = 1f;
    public float maxArcThickness = 1.6f;
    public float glowLerpSpeed = 5f;
    public float redlineThreshold = 0.92f;
    public float rpmShimmerIntensity = 0.15f;
    public float rpmShimmerSpeed = 20f;

    [Header("Arc Volumetric Haze")]
    public float hazeAlpha = 0.08f;
    public float hazeRadiusMultiplier = 1.08f;

    [Header("Glyph Settings")]
    public float glyphHeightOffset = 90f;
    public float glyphHorizontalOffset = 120f;
    public float glyphScaleFactor = 0.6f;
    public float glyphRotationSpeed = 20f;
    public float glyphShimmerIntensity = 2f;
    public float glyphShimmerDuration = 0.4f;

    [Header("Visual")]
    public GameObject starPrefab;
    public Material lineMaterial;


    List<Renderer> arcStars = new();
    List<Renderer> hazeStars = new();
    List<GameObject> gearStars = new();


    Transform arcRoot;
    Transform glyphRoot;


    int lastGear = -99;

    void Start()
    {
        CreateRoots();
        BuildArc();
        BuildHaze();
        CreateDrivingTexts();
    }

    void LateUpdate()
    {
        if (!vehicle || !mainCamera) return;

        AnchorToSky();

        UpdateReveal();

        UpdateArcFill();
        UpdateGlyph();
        UpdateDrivingTextUI();
    }

    void UpdateDrivingTextUI()
    {
        if (!vehicle || vehicle.rb == null) return;

        Vector3 vel = vehicle.rb.velocity;

        if (vel.sqrMagnitude < 1f)
        {
            FadeAllTexts(0f);
            return;
        }

        // ✅ USE ROAD DIRECTION (FIXED)
        Vector3 roadForward = layout.GetRoadDirectionAt(vehicle.transform.position);
        float alignment = Vector3.Dot(roadForward, vel.normalized);

        bool wrong = alignment < -0.2f;

        float rpmNorm = vehicle.engineRPM / vehicle.maxRPM;

        bool up =
            rpmNorm > 0.85f &&
            vehicle.currentGear > 0 &&
            vehicle.currentGear < vehicle.forwardGearRatios.Length;

        bool down =
            rpmNorm < 0.25f &&
            vehicle.currentGear > 1;

        // PRIORITY SYSTEM
        if (wrong)
        {
            wrongAlpha = Mathf.Lerp(wrongAlpha, 1f, Time.deltaTime * textFadeSpeed);
            upAlpha = Mathf.Lerp(upAlpha, 0f, Time.deltaTime * textFadeSpeed);
            downAlpha = Mathf.Lerp(downAlpha, 0f, Time.deltaTime * textFadeSpeed);
        }
        else
        {
            wrongAlpha = Mathf.Lerp(wrongAlpha, 0f, Time.deltaTime * textFadeSpeed);

            if (up && !down)
            {
                upAlpha = Mathf.Lerp(upAlpha, 1f, Time.deltaTime * textFadeSpeed);
                downAlpha = Mathf.Lerp(downAlpha, 0f, Time.deltaTime * textFadeSpeed);
            }
            else if (down && !up)
            {
                downAlpha = Mathf.Lerp(downAlpha, 1f, Time.deltaTime * textFadeSpeed);
                upAlpha = Mathf.Lerp(upAlpha, 0f, Time.deltaTime * textFadeSpeed);
            }
            else
            {
                upAlpha = Mathf.Lerp(upAlpha, 0f, Time.deltaTime * textFadeSpeed);
                downAlpha = Mathf.Lerp(downAlpha, 0f, Time.deltaTime * textFadeSpeed);
            }
        }

        ApplyText(wrongText, wrongMesh, wrongAlpha, Color.red, 1.3f);
        ApplyText(upshiftText, upMesh, upAlpha, Color.white, 1f);
        ApplyText(downshiftText, downMesh, downAlpha, Color.white, 1f);
    }

    string GetResetKeyLabel()
    {
        if (UnityEngine.InputSystem.Gamepad.current != null)
        {
            var name = UnityEngine.InputSystem.Gamepad.current.name.ToLower();

            if (name.Contains("dualshock") || name.Contains("dualsense"))
                return "△";

            return "Y"; // xbox default
        }

        return "R"; // keyboard fallback
    }

    void ApplyText(GameObject obj, TextMesh mesh, float alpha, Color color, float scaleMul)
    {
        if (!obj) return;

        Vector3 localPos =
            glyphRoot.localPosition +
            new Vector3(100f, 10f, 0f);

        obj.transform.localPosition = localPos;

        // 👉 face camera correctly
        obj.transform.rotation =
            Quaternion.LookRotation(
                obj.transform.position - mainCamera.transform.position
            );

        // 🔥 SCALE FIX: compensate for distance
        float distance =
            Vector3.Distance(mainCamera.transform.position, obj.transform.position);

        float scale =
            distance * 0.0025f; // THIS is the key

        obj.transform.localScale =
            Vector3.one * scale * scaleMul;

        // apply alpha
        Color c = color;
        c.a = alpha;

        mesh.color = c;
    }
    void FadeAllTexts(float target)
    {
        wrongAlpha = Mathf.Lerp(wrongAlpha, target, Time.deltaTime * textFadeSpeed);
        upAlpha = Mathf.Lerp(upAlpha, target, Time.deltaTime * textFadeSpeed);
        downAlpha = Mathf.Lerp(downAlpha, target, Time.deltaTime * textFadeSpeed);
    }
    void CreateDrivingTexts()
    {
        string key = GetResetKeyLabel();
        wrongText = CreateText("WRONG WAY • PRESS " + key, Color.red);
        upshiftText = CreateText("SHIFT UP ↑", Color.white);
        downshiftText = CreateText("SHIFT DOWN ↓", Color.white);

        wrongMesh = wrongText.GetComponent<TextMesh>();
        upMesh = upshiftText.GetComponent<TextMesh>();
        downMesh = downshiftText.GetComponent<TextMesh>();
    }
    GameObject CreateText(string content, Color color)
    {
        GameObject go = new GameObject(content);
        go.transform.parent = transform;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = content;
        tm.characterSize = textScale;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;

        tm.color = new Color(color.r, color.g, color.b, 0f);

        return go;
    }
    void UpdateReveal()
    {
        if (revealFactor < 1f)
        {
            revealTimer += Time.deltaTime;
            revealFactor = Mathf.Clamp01(revealTimer / revealDuration);
        }
    }

    void Update()
    {
        if (glyphRoot != null)
        {
            glyphRoot.Rotate(Vector3.forward,
                glyphRotationSpeed * Time.deltaTime,
                Space.Self);
        }
        if (wrongMesh != null)
        {
            wrongMesh.text = "WRONG WAY • PRESS " + GetResetKeyLabel();
        }
    }

    // ==================================================
    // SKY ANCHOR
    // ==================================================
    void AnchorToSky()
    {
        Vector3 camPos = mainCamera.transform.position;

        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;

        // 🔥 ORIGINAL POSITION (KEEP THIS)
        Vector3 basePos =
            camPos +
            forward * skyDistance +
            Vector3.up * skyHeight;

        // 🔥 SMALL HORIZONTAL OFFSET (FIX DRIFT)
        float horizontalOffset = -20; // tweak 50–120

        Vector3 targetPos =
            basePos + right * horizontalOffset;

        transform.position =
            Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);

        transform.rotation =
            Quaternion.LookRotation(
                transform.position - camPos
            );
    }

    // ==================================================
    // ROOT OBJECTS
    // ==================================================
    void CreateRoots()
    {
        arcRoot = new GameObject("ArcRoot").transform;
        arcRoot.parent = transform;

        arcRoot.localPosition =
            Vector3.right * arcHorizontalOffset +
            Vector3.up * arcVerticalOffset;

        arcRoot.localRotation = Quaternion.identity;
        arcRoot.localScale = Vector3.one * arcScaleMultiplier;

        glyphRoot = new GameObject("GlyphRoot").transform;
        glyphRoot.parent = transform;

        glyphRoot.localPosition =
            Vector3.right * glyphHorizontalOffset +
            Vector3.up * (arcVerticalCurve + glyphHeightOffset);

        glyphRoot.localRotation = Quaternion.identity;
    }

    // ==================================================
    // CLEAN ARC BUILD
    // ==================================================
    void BuildArc()
    {
        for (int layer = 0; layer < arcLayers; layer++)
        {
            float depth = layer * arcLayerDepthSpacing;

            for (int i = 0; i < arcStarCount; i++)
            {
                float t = (float)i / (arcStarCount - 1);
                float angle = Mathf.Lerp(-arcAngle * 0.5f, arcAngle * 0.5f, t);

                Vector3 dir =
                    Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                float vertical =
                    Mathf.Sin(t * Mathf.PI) * arcVerticalCurve;

                Vector3 pos =
                    dir * arcRadius +
                    Vector3.up * vertical -
                    Vector3.forward * depth;

                GameObject star =
                    Instantiate(starPrefab, arcRoot);

                star.transform.localPosition = pos;
                star.transform.localScale =
                    Vector3.one * Mathf.Lerp(4f, 2.5f, (float)layer / arcLayers);

                arcStars.Add(star.GetComponent<Renderer>());
            }
        }
    }

    // ==================================================
    // VOLUMETRIC HAZE
    // ==================================================
    void BuildHaze()
    {
        for (int i = 0; i < arcStarCount; i++)
        {
            float t = (float)i / (arcStarCount - 1);
            float angle = Mathf.Lerp(-arcAngle * 0.5f, arcAngle * 0.5f, t);

            Vector3 dir =
                Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            float vertical =
                Mathf.Sin(t * Mathf.PI) * arcVerticalCurve;

            Vector3 pos =
                dir * arcRadius * hazeRadiusMultiplier +
                Vector3.up * vertical;

            GameObject star =
                Instantiate(starPrefab, arcRoot);

            star.transform.localPosition = pos;
            star.transform.localScale = Vector3.one * 6f;

            Renderer r = star.GetComponent<Renderer>();
            Color c = r.material.color;
            c.a = hazeAlpha;
            r.material.color = c;

            hazeStars.Add(r);
        }
    }




    // ==================================================
    // SPEED FILL SYSTEM
    // ==================================================
    // ==================================================
    // SPEED FILL SYSTEM (CLEAN WHITE + REDLINE PULSE)
    // ==================================================
    void UpdateArcFill()
    {
        float speedNorm =
            Mathf.Clamp01(vehicle.rb.velocity.magnitude / vehicle.maxSpeed);

        float rpmNorm =
            Mathf.Clamp01(vehicle.engineRPM / vehicle.maxRPM);

        bool suggestUpshift =
    rpmNorm > 0.85f &&
    vehicle.currentGear > 0 &&
    vehicle.currentGear < vehicle.forwardGearRatios.Length;

        bool suggestDownshift =
            rpmNorm < 0.25f &&
            vehicle.currentGear > 1;

        int litCount =
            Mathf.RoundToInt(speedNorm * arcStarCount);

        float thickness =
            Mathf.Lerp(baseArcThickness, maxArcThickness, speedNorm);

        bool inRedline = rpmNorm > redlineThreshold;

        // periodic pulse when redlining (0.5–1 sec rhythm)
        float pulse = 0f;
        if (inRedline)
        {
            pulse =
                Mathf.Sin(Time.time * 6f) * 0.5f + 0.5f; // 0–1 wave
        }

        for (int i = 0; i < arcStars.Count; i++)
        {
            int indexWithinLayer = i % arcStarCount;
            bool lit = indexWithinLayer < litCount;

            float baseAlpha = lit ? 1f : 0.05f;

            // Subtle rpm shimmer (always white)
            float shimmer =
                Mathf.Sin(Time.time * rpmShimmerSpeed + i * 0.3f)
                * rpmNorm * rpmShimmerIntensity;

            float shiftPulse = 0f;

            if (suggestUpshift && lit)
            {
                shiftPulse = Mathf.Sin(Time.time * 8f) * 0.3f;
            }

            if (suggestDownshift && lit)
            {
                shiftPulse = Mathf.Sin(Time.time * 6f) * 0.2f;
            }

            float targetAlpha =
                (baseAlpha + shimmer + shiftPulse) * revealFactor;


            // Redline pulse (purely brightness + slight thickness)
            if (inRedline && lit)
            {
                float pulseBoost = pulse * 0.35f; // noticeable but not dramatic
                targetAlpha += pulseBoost;
            }

            Color c = arcStars[i].material.color;

            // Always white stars
            c.r = 1f;
            c.g = 1f;
            c.b = 1f;

            c.a = Mathf.Lerp(c.a,
                Mathf.Clamp01(targetAlpha),
                Time.deltaTime * glowLerpSpeed);

            arcStars[i].material.color = c;

            // Thickness swell during redline pulse
            float thicknessBoost = inRedline ? pulse * 0.15f : 0f;

            Vector3 scale = arcStars[i].transform.localScale;
            scale.y = (thickness + thicknessBoost) * scale.x;
            arcStars[i].transform.localScale = scale;
        }
    }




    // ==================================================
    // GLYPH SYSTEM (unchanged)
    // ==================================================
    void UpdateGlyph()
    {
        if (vehicle.currentGear == lastGear) return;

        lastGear = vehicle.currentGear;

        ClearGlyph();

        if (vehicle.currentGear <= 0) return;

        CreateGlyph(vehicle.currentGear);
        StartCoroutine(GlyphShimmer());
    }

    void CreateGlyph(int gear)
    {
        float size = arcRadius * 0.05f * glyphScaleFactor;

        List<Vector3> points = GetGlyphPattern(gear, size);

        foreach (Vector3 p in points)
        {
            GameObject star =
                Instantiate(starPrefab, glyphRoot);

            star.transform.localPosition = p;
            star.transform.localScale =
                Vector3.one * (arcRadius * 0.012f * glyphScaleFactor);

            gearStars.Add(star);
        }


    }

    IEnumerator GlyphShimmer()
    {
        float timer = 0f;

        while (timer < glyphShimmerDuration)
        {
            timer += Time.deltaTime;

            float pulse =
                Mathf.Sin(timer * 20f) * glyphShimmerIntensity;

            foreach (var s in gearStars)
            {
                s.transform.localScale =
                    Vector3.one *
                    (arcRadius * 0.012f * glyphScaleFactor + pulse * 0.002f);
            }

            yield return null;
        }
    }

    List<Vector3> GetGlyphPattern(int gear, float size)
    {
        List<Vector3> points = new();

        switch (gear)
        {
            case 1:
                points.Add(Vector3.zero);
                break;
            case 2:
                points.Add(Vector3.left * size);
                points.Add(Vector3.right * size);
                break;
            case 3:
                points.Add(Vector3.up * size);
                points.Add(Vector3.left * size);
                points.Add(Vector3.right * size);
                break;
            case 4:
                points.Add(Vector3.up * size);
                points.Add(Vector3.right * size);
                points.Add(Vector3.down * size);
                points.Add(Vector3.left * size);
                break;
            default:
                for (int i = 0; i < Mathf.Min(gear, 6); i++)
                {
                    float angle = i * Mathf.PI * 2f / Mathf.Min(gear, 6);
                    points.Add(new Vector3(Mathf.Cos(angle),
                                           Mathf.Sin(angle), 0f) * size);
                }
                break;
        }

        return points;
    }


    void ClearGlyph()
    {
        foreach (var s in gearStars) Destroy(s);


        gearStars.Clear();

    }
}
