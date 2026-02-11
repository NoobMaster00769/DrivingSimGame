using UnityEngine;

public class PlayerDriveMetrics : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public VehicleContext context;

    [Header("Primary Outputs")]
    [Range(0, 1)] public float intensity;
    [Range(0, 1)] public float controlQuality;
    [Range(0, 1)] public float rhythm;
    [Range(0, 1)] public float flow;

    [Header("Tuning")]
    public float smoothing = 2f;

    float steeringHistory;
    float throttleHistory;
    float rhythmAccumulator;
    float flowTimer;

    void FixedUpdate()
    {
        if (!rb || !context) return;

        float steering = context.input.Steering;
        float throttle = context.input.Throttle;
        float speed = rb.velocity.magnitude;

        // -------------------------
        // INTENSITY
        // -------------------------
        float rpmNorm = context.engineRPM / context.maxRPM;
        float speedNorm = speed / context.maxSpeed;

        intensity = Mathf.Clamp01(
            rpmNorm * 0.6f +
            speedNorm * 0.4f
        );

        // -------------------------
        // CONTROL QUALITY
        // -------------------------
        float lateralVel =
            Mathf.Abs(Vector3.Dot(rb.velocity, transform.right));

        float drift = Mathf.Clamp01(lateralVel / 12f);

        controlQuality =
            1f - Mathf.Clamp01(
                Mathf.Abs(steering) * 0.4f +
                drift * 0.6f
            );

        // -------------------------
        // RHYTHM DETECTION
        // -------------------------
        float steeringDelta =
            Mathf.Abs(steering - steeringHistory);

        float throttleDelta =
            Mathf.Abs(throttle - throttleHistory);

        float smoothOscillation =
            1f - Mathf.Clamp01(steeringDelta * 5f);

        rhythmAccumulator =
            Mathf.Lerp(rhythmAccumulator,
                       smoothOscillation,
                       Time.fixedDeltaTime * smoothing);

        rhythm = rhythmAccumulator;

        steeringHistory = steering;
        throttleHistory = throttle;

        // -------------------------
        // FLOW DETECTION
        // -------------------------
        if (controlQuality > 0.8f &&
            rhythm > 0.7f &&
            intensity < 0.7f)
        {
            flowTimer += Time.fixedDeltaTime;
        }
        else
        {
            flowTimer =
                Mathf.Max(0f,
                          flowTimer - Time.fixedDeltaTime * 2f);
        }

        flow = Mathf.Clamp01(flowTimer / 5f);
    }
}