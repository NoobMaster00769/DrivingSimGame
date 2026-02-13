using UnityEngine;

public class FakeElevationIllusion : MonoBehaviour
{
    public RoadState roadState;

    [Header("Illusion Settings")]
    public float maxPitch = 5f;
    public float waveSpeed = 0.5f;
    public float smoothSpeed = 3f;

    private float currentPitch;
    private float waveTime;

    void Update()
    {
        if (!roadState) return;

        // Dynamic hill wave (not static)
        waveTime += Time.deltaTime * waveSpeed;

        float wave =
     Mathf.Sin(waveTime * (1f + roadState.elevationEnergy))
     * roadState.elevationEnergy;


        float targetPitch =
            wave * maxPitch;

        currentPitch = Mathf.Lerp(
            currentPitch,
            targetPitch,
            Time.deltaTime * smoothSpeed
        );

        // Proper rotation (no jitter)
        transform.localRotation =
            Quaternion.Euler(currentPitch, 0f, 0f);
    }
}
