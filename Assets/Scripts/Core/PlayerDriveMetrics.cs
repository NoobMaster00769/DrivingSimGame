using UnityEngine;

public class PlayerDriveMetrics : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public VehicleContext context;

    [Header("Primary Outputs")]
    [Range(0, 1)] public float aggression;
    [Range(0, 1)] public float engineStress;
    [Range(0, 1)] public float gearMistake;
    [Range(0, 1)] public float smoothness;

    [Header("Weights")]
    public float steeringWeight = 0.25f;
    public float throttleWeight = 0.15f;
    public float driftWeight = 0.25f;
    public float rpmWeight = 0.2f;
    public float gearWeight = 0.25f;

    [Header("Smoothing")]
    public float smoothing = 2f;

    float redlineTimer;
    float smoothAggression;

    void FixedUpdate()
    {
        if (!rb || !context) return;

        // ================= CONTROL STRESS =================
        float steering = Mathf.Abs(context.input.Steering);
        float throttle = context.input.Throttle;

        float lateralVel =
            Mathf.Abs(Vector3.Dot(rb.velocity, transform.right));
        float drift = Mathf.Clamp01(lateralVel / 10f);

        float controlStress =
            steering * steeringWeight +
            throttle * throttleWeight +
            drift * driftWeight;

        smoothness = 1f - Mathf.Clamp01(controlStress);

        // ================= ENGINE STRESS =================
        float rpmNorm = context.engineRPM / context.maxRPM;

        if (rpmNorm > 0.9f)
            redlineTimer += Time.fixedDeltaTime;
        else
            redlineTimer = Mathf.Max(0f, redlineTimer - Time.fixedDeltaTime * 2f);

        engineStress = Mathf.Clamp01(redlineTimer * 0.5f);

        // ================= GEAR QUALITY =================
        float speed = rb.velocity.magnitude;

        float optimalRPM =
            Mathf.Lerp(
                context.optimalDownshiftRPM,
                context.optimalUpshiftRPM,
                speed / context.maxSpeed
            );

        gearMistake =
            Mathf.Clamp01(
                Mathf.Abs(context.engineRPM - optimalRPM) / context.maxRPM
            );

        // ================= FINAL AGGRESSION =================
        float rawAggression =
            controlStress +
            engineStress * rpmWeight +
            gearMistake * gearWeight;

        smoothAggression = Mathf.Lerp(
            smoothAggression,
            rawAggression,
            Time.fixedDeltaTime * smoothing
        );

        aggression = Mathf.Clamp01(smoothAggression);
    }
}
