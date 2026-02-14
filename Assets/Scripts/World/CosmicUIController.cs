using System.Collections.Generic;
using UnityEngine;

public class CosmicUIController : MonoBehaviour
{
    [Header("References")]
    public VehicleContext vehicle;
    public Camera mainCamera;

    [Header("Sky Placement")]
    public float skyDistance = 1000f;
    public float skyHeight = 300f;

    [Header("Arc Settings")]
    public int arcStarCount = 60;
    public float arcRadius = 180f;
    public float arcAngle = 80f;
    public float arcThickness = 12f;     // subtle vertical spread

    [Header("Visual")]
    public GameObject starPrefab;
    public Material lineMaterial;
    public GameObject nebulaPrefab;
    public float glowSpeed = 4f;

    [Header("Celestial Drift")]
    public float driftSpeed = 0.08f;
    public float driftAmount = 2f;

    List<Renderer> arcStars = new();
    List<GameObject> gearStars = new();
    List<LineRenderer> gearLines = new();

    int lastGear = -99;
    float driftTimer;

    GameObject nebulaInstance;

    void Start()
    {
        BuildArc();
        CreateNebula();
    }

    void LateUpdate()
    {
        if (!vehicle || !mainCamera) return;

        AnchorToSky();
        ApplyDrift();

        UpdateArcFill();
        UpdateGearConstellation();
    }

    // ============================================
    // SKY ANCHOR
    // ============================================
    void AnchorToSky()
    {
        transform.position =
            mainCamera.transform.position +
            mainCamera.transform.forward * skyDistance +
            Vector3.up * skyHeight;

        transform.rotation =
            Quaternion.LookRotation(
                transform.position - mainCamera.transform.position
            );
    }

    // ============================================
    // Gentle celestial drift
    // ============================================
    void ApplyDrift()
    {
        driftTimer += Time.deltaTime * driftSpeed;
        float drift = Mathf.Sin(driftTimer) * driftAmount;

        transform.Rotate(Vector3.up, drift * Time.deltaTime, Space.World);
    }

    // ============================================
    // BUILD ARC (structured celestial band)
    // ============================================
    void BuildArc()
    {
        for (int i = 0; i < arcStarCount; i++)
        {
            float t = (float)i / (arcStarCount - 1);
            float angle = Mathf.Lerp(-arcAngle * 0.5f, arcAngle * 0.5f, t);

            Vector3 dir =
                Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            // slight vertical variation only
            float verticalNoise =
                Mathf.Sin(t * Mathf.PI) * arcThickness;

            Vector3 localPos =
                dir * arcRadius +
                Vector3.up * verticalNoise;

            GameObject star =
                Instantiate(starPrefab, transform);

            star.transform.localPosition = localPos;
            star.transform.localScale = Vector3.one * 6f;
            star.transform.localRotation = Quaternion.identity;

            arcStars.Add(star.GetComponent<Renderer>());
        }
    }

    // ============================================
    // SPEED FILL
    // ============================================
    void UpdateArcFill()
    {
        float speedNorm =
            Mathf.Clamp01(vehicle.rb.velocity.magnitude / vehicle.maxSpeed);

        int litCount =
            Mathf.RoundToInt(speedNorm * arcStarCount);

        for (int i = 0; i < arcStars.Count; i++)
        {
            float target =
                i < litCount ? 1f : 0.08f;

            Color c = arcStars[i].material.color;
            c.a = Mathf.Lerp(c.a, target, Time.deltaTime * glowSpeed);
            arcStars[i].material.color = c;
        }
    }

    // ============================================
    // GEAR CONSTELLATION (perfectly aligned)
    // ============================================
    void UpdateGearConstellation()
    {
        if (vehicle.currentGear == lastGear) return;

        lastGear = vehicle.currentGear;

        foreach (var s in gearStars) Destroy(s);
        foreach (var l in gearLines) Destroy(l.gameObject);

        gearStars.Clear();
        gearLines.Clear();

        if (vehicle.currentGear <= 0) return;

        float size = arcRadius * 0.07f; // smaller now
        Vector3 baseOffset =
            Vector3.up * (arcThickness + 10f); // sits right above arc

        switch (vehicle.currentGear)
        {
            case 1: CreateVertical(size, baseOffset); break;
            case 2: CreateBinary(size, baseOffset); break;
            case 3: CreatePolygon(size, 3, baseOffset); break;
            case 4: CreatePolygon(size, 4, baseOffset); break;
            case 5: CreatePolygon(size, 5, baseOffset); break;
            default: CreatePolygon(size, 6, baseOffset); break;
        }

        CreateLinesBetweenStars();
    }

    void CreatePolygon(float size, int sides, Vector3 baseOffset)
    {
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;

            Vector3 offset =
                new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * size;

            CreateStar(baseOffset + offset);
        }
    }

    void CreateVertical(float s, Vector3 baseOffset)
    {
        CreateStar(baseOffset);
        CreateStar(baseOffset + Vector3.up * s);
    }

    void CreateBinary(float s, Vector3 baseOffset)
    {
        CreateStar(baseOffset + Vector3.left * s * 0.5f);
        CreateStar(baseOffset + Vector3.right * s * 0.5f);
    }

    void CreateStar(Vector3 localPos)
    {
        GameObject star =
            Instantiate(starPrefab, transform);

        star.transform.localPosition = localPos;
        star.transform.localScale = Vector3.one * (arcRadius * 0.02f);
        star.transform.localRotation = Quaternion.identity;

        gearStars.Add(star);
    }

    // ============================================
    // Constellation Lines
    // ============================================
    void CreateLinesBetweenStars()
    {
        if (gearStars.Count < 2) return;

        for (int i = 0; i < gearStars.Count; i++)
        {
            int next = (i + 1) % gearStars.Count;

            GameObject lineObj = new GameObject("ConstellationLine");
            lineObj.transform.parent = transform;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.positionCount = 2;
            lr.startWidth = arcRadius * 0.003f;
            lr.endWidth = arcRadius * 0.003f;
            lr.useWorldSpace = false;

            lr.SetPosition(0, gearStars[i].transform.localPosition);
            lr.SetPosition(1, gearStars[next].transform.localPosition);

            Color faint = new Color(1f, 1f, 1f, 0.15f);
            lr.startColor = faint;
            lr.endColor = faint;

            gearLines.Add(lr);
        }
    }

    // ============================================
    // Nebula Background
    // ============================================
    void CreateNebula()
    {
        if (!nebulaPrefab) return;

        nebulaInstance =
            Instantiate(nebulaPrefab, transform);

        nebulaInstance.transform.localPosition =
            Vector3.zero;

        nebulaInstance.transform.localScale =
            Vector3.one * arcRadius * 1.8f;

        nebulaInstance.transform.localRotation =
            Quaternion.identity;
    }
}
