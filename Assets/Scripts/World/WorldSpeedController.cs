using UnityEngine;

public class WorldSpeedController : MonoBehaviour
{
    public WorldDrift drift;

    [Header("Base Speed")]
    public float baseSpeed = 6f;

    [Header("Breathing")]
    public float breathAmplitude = 0.6f;
    public float breathSpeed = 0.2f;

    void Update()
    {
        float breath = Mathf.Sin(Time.time * breathSpeed) * breathAmplitude;
        drift.speed = baseSpeed + breath;
    }
}
