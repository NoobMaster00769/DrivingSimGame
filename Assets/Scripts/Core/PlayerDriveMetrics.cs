using UnityEngine;

public class PlayerDriveMetrics : MonoBehaviour
{
    public Rigidbody rb;
    public VehicleContext context;

    [Range(0, 1)] public float aggression;

    [Header("Tuning")]
    public float steeringWeight = 0.4f;
    public float throttleWeight = 0.3f;
    public float driftWeight = 0.3f;
    public float smoothing = 2f;

    float smoothAggression;

    void FixedUpdate()
    {
        if (!rb || !context) return;

        float steering = Mathf.Abs(context.input.Steering);
        float throttle = context.input.Throttle;

        float lateralVel = Mathf.Abs(Vector3.Dot(rb.velocity, transform.right));
        float drift = Mathf.Clamp01(lateralVel / 10f);

        float raw =
            steering * steeringWeight +
            throttle * throttleWeight +
            drift * driftWeight;

        smoothAggression = Mathf.Lerp(
            smoothAggression,
            raw,
            Time.fixedDeltaTime * smoothing
        );

        aggression = Mathf.Clamp01(smoothAggression);
    }
}
