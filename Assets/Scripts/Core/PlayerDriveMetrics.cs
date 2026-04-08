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


        float rpmNorm = context.engineRPM / context.maxRPM;
        float speedNorm = speed / context.maxSpeed;

        intensity = Mathf.Clamp01(
            rpmNorm * 0.5f +
            speedNorm * 0.5f
        );


        float lateralVel =
            Mathf.Abs(Vector3.Dot(rb.velocity, transform.right));

        float drift = Mathf.Clamp01(lateralVel / 15f); 

        controlQuality =
            1f - Mathf.Clamp01(
                Mathf.Abs(steering) * 0.35f +
                drift * 0.65f
            );


        float steeringDelta =
            Mathf.Abs(steering - steeringHistory);

        float smoothOscillation =
            1f - Mathf.Clamp01(steeringDelta * 4f);

        rhythmAccumulator =
            Mathf.Lerp(rhythmAccumulator,
                       smoothOscillation,
                       Time.fixedDeltaTime * smoothing);

        rhythm = rhythmAccumulator;

        steeringHistory = steering;
        throttleHistory = throttle;


        if (controlQuality > 0.7f &&
            rhythm > 0.6f &&
            intensity < 0.85f)
        {
            flowTimer += Time.fixedDeltaTime;
        }
        else
        {
            flowTimer =
                Mathf.Max(0f,
                          flowTimer - Time.fixedDeltaTime * 1.2f);
        }

        flow = Mathf.Clamp01(flowTimer / 3.5f);  // was /5
    }
}
