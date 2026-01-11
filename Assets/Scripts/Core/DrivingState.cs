using UnityEngine;

public class DrivingState : VehicleState
{
    public DrivingState(VehicleStateMachine sm, VehicleContext ctx)
        : base(sm, ctx) { }

    public override void FixedUpdate()
    {
        UpdateEngineRPM();
        ApplyMotorTorque();
        ApplySteering();
        ApplyBrakes();
        ApplyFriction();
        UpdateWheelVisuals();
    }

    // ---------------- ENGINE RPM ----------------
    void UpdateEngineRPM()
    {
        float rearRPM =
            (context.colliders.RRWheel.rpm + context.colliders.RLWheel.rpm) * 0.5f;

        rearRPM = Mathf.Abs(rearRPM);

        context.engineRPM = Mathf.Lerp(
            context.engineRPM,
            Mathf.Max(context.idleRPM, rearRPM),
            Time.fixedDeltaTime * 5f
        );

        context.engineRPM = Mathf.Clamp(context.engineRPM, context.idleRPM, context.maxRPM);
    }

    // ---------------- MOTOR ----------------
    void ApplyMotorTorque()
    {
        float throttle = context.input.Throttle;

        if (context.rb.velocity.magnitude > context.maxSpeed)
        {
            context.colliders.RRWheel.motorTorque = 0f;
            context.colliders.RLWheel.motorTorque = 0f;
            return;
        }

        float normalizedRPM = context.engineRPM / context.maxRPM;
        float torqueMultiplier = context.torqueCurve.Evaluate(normalizedRPM);

        float torque = throttle * context.maxMotorTorque * torqueMultiplier;

        context.colliders.RRWheel.motorTorque = torque;
        context.colliders.RLWheel.motorTorque = torque;
    }

    // ---------------- STEERING ----------------
    void ApplySteering()
    {
        float steering = context.input.Steering;
        float speed = context.rb.velocity.magnitude;

        float steerLimit = Mathf.Lerp(
            context.maxSteerAngle,
            context.maxSteerAngle * 0.35f,
            speed / context.maxSpeed
        );

        float target = steering * steerLimit;

        context.colliders.FLWheel.steerAngle =
            Mathf.Lerp(context.colliders.FLWheel.steerAngle, target,
                Time.fixedDeltaTime * context.steerResponse);

        context.colliders.FRWheel.steerAngle =
            context.colliders.FLWheel.steerAngle;
    }

    // ---------------- BRAKES ----------------
    void ApplyBrakes()
    {
        float brake = context.input.Brake;
        float brakeTorque = brake * context.brakeForce;

        context.colliders.FLWheel.brakeTorque = brakeTorque;
        context.colliders.FRWheel.brakeTorque = brakeTorque;
        context.colliders.RLWheel.brakeTorque = brakeTorque;
        context.colliders.RRWheel.brakeTorque = brakeTorque;
    }

    // ---------------- FRICTION ----------------
    void ApplyFriction()
    {
        bool handbrake = context.input.Brake > 0.1f;

        WheelFrictionCurve rear = context.colliders.RRWheel.sidewaysFriction;
        rear.stiffness = handbrake
            ? context.rearHandbrakeFriction
            : context.rearSideFriction;

        context.colliders.RRWheel.sidewaysFriction = rear;
        context.colliders.RLWheel.sidewaysFriction = rear;
    }

    // ---------------- VISUALS ----------------
    void UpdateWheelVisuals()
    {
        UpdateWheel(context.colliders.FLWheel, context.FL_WheelMesh);
        UpdateWheel(context.colliders.FRWheel, context.FR_WheelMesh);
        UpdateWheel(context.colliders.RLWheel, context.RL_WheelMesh);
        UpdateWheel(context.colliders.RRWheel, context.RR_WheelMesh);
    }

    void UpdateWheel(WheelCollider col, Transform wheelMesh)
    {
        Vector3 pos;
        Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        wheelMesh.position = pos;
        wheelMesh.rotation = rot;
    }
}
