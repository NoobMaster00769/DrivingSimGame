using UnityEngine;
using System.Collections.Generic;

public class WorldEventDirector : MonoBehaviour
{
    public RoadState road;

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

            default:
                road.arcAmplitude = 0.8f;
                road.arcFrequency = 1.0f;
                road.rhythmIntensity = 1.0f;
                break;
        }
    }
}
