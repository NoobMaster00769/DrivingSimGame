using UnityEngine;
using System.Collections.Generic;

enum ArcPhase
{
    Calm,
    Flow,
    Intense
}

public class WorldEventDirector : MonoBehaviour
{
    public RoadState road;

    ArcPhase currentPhase;

    [Header("Arc Settings")]
    public float arcDuration = 120f;
    public float miniArcMin = 15f;
    public float miniArcMax = 30f;

    float arcTimer;
    float miniArcTimer;
    float miniArcDuration;

    int currentArcIndex;
    int currentMiniArc;

    List<int> arcOrder = new();

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
    }

    public string CurrentArcName
    {
        get
        {
            switch (currentArcIndex)
            {
                case 0: return "Calm";
                case 1: return "Pulse";
                case 2: return "Ribbon";
                case 3: return "Chaotic";
                case 4: return "Dream";
                case 5: return "Surge";
                case 6: return "Drift";
            }
            return "Unknown";
        }
    }

    void GenerateArcOrder()
    {
        arcOrder.Clear();

        switch (currentPhase)
        {
            case ArcPhase.Calm:
                arcOrder.AddRange(new int[] { 0, 4, 6 });
                currentPhase = ArcPhase.Flow;
                break;

            case ArcPhase.Flow:
                arcOrder.AddRange(new int[] { 1, 2, 6 });
                currentPhase = ArcPhase.Intense;
                break;

            case ArcPhase.Intense:
                arcOrder.AddRange(new int[] { 3, 5, 2 });
                currentPhase = ArcPhase.Calm;
                break;
        }

        // shuffle inside phase
        for (int i = 0; i < arcOrder.Count; i++)
        {
            int r = Random.Range(i, arcOrder.Count);
            (arcOrder[i], arcOrder[r]) = (arcOrder[r], arcOrder[i]);
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

        ApplyArcPersonality();
    }

    void ApplyArcPersonality()
    {
        switch (currentArcIndex)
        {
            case 0: // Calm
                road.arcAmplitude = 0.6f;
                road.arcFrequency = 0.6f;
                road.rhythmIntensity = 0.7f;
                break;

            case 1: // Pulse
                road.arcAmplitude = 0.9f;
                road.arcFrequency = 1.0f;
                road.rhythmIntensity = 1.0f;
                break;

            case 2: // Ribbon
                road.arcAmplitude = 1.1f;
                road.arcFrequency = 1.3f;
                road.rhythmIntensity = 1.2f;
                break;

            case 3: // Chaotic
                road.arcAmplitude = 1.4f;
                road.arcFrequency = 1.6f;
                road.rhythmIntensity = 1.5f;
                break;

            case 4: // Dream (very meditative)
                road.arcAmplitude = 0.45f;
                road.arcFrequency = 0.4f;
                road.rhythmIntensity = 0.55f;
                break;

            case 5: // Surge (long sweeping curves)
                road.arcAmplitude = 1.2f;
                road.arcFrequency = 0.8f;
                road.rhythmIntensity = 1.35f;
                break;

            case 6: // Drift (slow drifting road)
                road.arcAmplitude = 0.8f;
                road.arcFrequency = 0.35f;
                road.rhythmIntensity = 0.8f;
                break;
        }
    }
}
