using UnityEngine;

public class CelestialDrift : MonoBehaviour
{
    public float driftAmount = 0.3f;
    public float driftSpeed = 0.15f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float drift =
            Mathf.Sin(Time.time * driftSpeed) * driftAmount;

        transform.localPosition =
            startPos + Vector3.up * drift;
    }
}