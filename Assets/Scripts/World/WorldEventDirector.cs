using UnityEngine;
using System.Collections.Generic;

public class WorldEventDirector : MonoBehaviour
{
    public RoadState road;

    [Header("Arc Settings")]
    public float arcDuration = 480f; // 8 minutes
    public float miniArcMin = 60f;
    public float miniArcMax = 120f;
    public float blendSpeed = 0.5f;

    float arcTimer;
    float miniArcTimer;
    float miniArcDuration;

    int currentArcIndex;
    int currentMiniArc;

    List<int> arcOrder = new();

    float targetCurvature;
    float targetWidth;
    float targetBanking;

    // 🔥 PUBLIC ACCESSORS (FIX)
    public int CurrentArcIndex => currentArcIndex;
    public int CurrentMiniArc => currentMiniArc;

    void Start()
    {
        GenerateArcOrder();
        BeginNewArc();
    }

    void Update()
    {
        if (!road) return;

        arcTimer += Time.deltaTime;
        miniArcTimer += Time.deltaTime;

        if (miniArcTimer > miniArcDuration)
            PickMiniArc();

        if (arcTimer > arcDuration)
            BeginNewArc();

        ApplyTargets();
    }

    void GenerateArcOrder()
    {
        arcOrder.Clear();

        List<int> temp = new() { 0, 1, 2, 3, 4, 5, 6 };

        while (temp.Count > 0)
        {
            int r = Random.Range(0, temp.Count);
            arcOrder.Add(temp[r]);
            temp.RemoveAt(r);
        }
    }

    void BeginNewArc()
    {
        arcTimer = 0f;
        miniArcTimer = 0f;

        if (arcOrder.Count == 0)
            GenerateArcOrder();

        currentArcIndex = arcOrder[0];
        arcOrder.RemoveAt(0);

        PickMiniArc();
    }

    void PickMiniArc()
    {
        miniArcTimer = 0f;
        miniArcDuration = Random.Range(miniArcMin, miniArcMax);
        currentMiniArc = Random.Range(0, 3);
        SetArcTargets();
    }

    void SetArcTargets()
    {
        float randomness = Random.Range(0.9f, 1.1f);

        switch (currentArcIndex)
        {
            case 0: // HORIZON STRETCH
                targetCurvature = Mathf.Lerp(0.4f, 0.6f, currentMiniArc * 0.5f);
                targetWidth = 1.2f;
                targetBanking = 0.6f;
                break;

            case 1: // PULSEFIELD
                targetCurvature = 0.6f;
                targetWidth = Mathf.Lerp(0.9f, 1.1f,
                    Mathf.Sin(Time.time * 0.2f) * 0.5f + 0.5f);
                targetBanking = 0.7f;
                break;

            case 2: // RIBBON WAVE
                targetCurvature = Mathf.Lerp(0.6f, 1.0f, currentMiniArc * 0.5f);
                targetWidth = 0.8f;
                targetBanking = 0.9f;
                break;

            case 3: // FRACTAL DRIFT
                targetCurvature = Random.Range(0.5f, 1.2f);
                targetWidth = Random.Range(0.75f, 1.0f);
                targetBanking = Random.Range(0.6f, 1.0f);
                break;

            case 4: // SYMMETRY LOOP
                targetCurvature = 0.8f;
                targetWidth = 0.85f;
                targetBanking = 0.8f;
                break;

            case 5: // BREATHING TUNNEL
                float breath = Mathf.Sin(Time.time * 0.4f) * 0.5f + 0.5f;
                targetCurvature = 0.7f;
                targetWidth = Mathf.Lerp(0.7f, 1.2f, breath);
                targetBanking = 0.6f + breath * 0.3f;
                break;

            case 6: // LIQUID SPIRAL
                float spiral = Mathf.PingPong(Time.time * 0.1f, 1f);
                targetCurvature = Mathf.Lerp(0.6f, 1.3f, spiral);
                targetWidth = 0.9f;
                targetBanking = Mathf.Lerp(0.6f, 1.1f, spiral);
                break;
        }

        targetCurvature *= randomness;
        targetWidth *= randomness;
        targetBanking *= randomness;
    }

    void ApplyTargets()
    {
        road.curvatureMultiplier =
            Mathf.Lerp(road.curvatureMultiplier,
                       targetCurvature,
                       Time.deltaTime * blendSpeed);

        road.widthMultiplier =
            Mathf.Lerp(road.widthMultiplier,
                       targetWidth,
                       Time.deltaTime * blendSpeed);

        road.bankingMultiplier =
            Mathf.Lerp(road.bankingMultiplier,
                       targetBanking,
                       Time.deltaTime * blendSpeed);
    }
}
