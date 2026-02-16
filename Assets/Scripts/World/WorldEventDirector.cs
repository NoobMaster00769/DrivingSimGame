using UnityEngine;
using System.Collections.Generic;

public class WorldEventDirector : MonoBehaviour
{
    public RoadState road;

    [Header("Arc Settings")]
    public float arcDuration = 180f;     // shorter arcs = more variation
    public float miniArcMin = 20f;
    public float miniArcMax = 40f;

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
            case 0: // Calm Horizon
                road.arcAmplitude = 0.6f;
                road.arcFrequency = 0.6f;
                road.arcWidthTarget = 0.9f;
                road.arcBankTarget = 0.6f;
                break;

            case 1: // Pulse
                road.arcAmplitude = 0.9f;
                road.arcFrequency = 1.2f;
                road.arcWidthTarget = 0.8f;
                road.arcBankTarget = 0.8f;
                break;

            case 2: // Ribbon
                road.arcAmplitude = 1.1f;
                road.arcFrequency = 1.5f;
                road.arcWidthTarget = 0.7f;
                road.arcBankTarget = 0.9f;
                break;

            case 3: // Chaotic Drift
                road.arcAmplitude = 1.3f;
                road.arcFrequency = 2.0f;
                road.arcWidthTarget = 0.65f;
                road.arcBankTarget = 1.0f;
                break;

            case 4:
                road.arcAmplitude = 0.8f;
                road.arcFrequency = 1.3f;
                road.arcWidthTarget = 0.75f;
                road.arcBankTarget = 0.85f;
                break;

            case 5:
                road.arcAmplitude = 0.6f;
                road.arcFrequency = 0.9f;
                road.arcWidthTarget = 1.0f;
                road.arcBankTarget = 0.7f;
                break;

            case 6:
                road.arcAmplitude = 1.5f;
                road.arcFrequency = 2.2f;
                road.arcWidthTarget = 0.6f;
                road.arcBankTarget = 1.1f;
                break;
        }
    }
}
