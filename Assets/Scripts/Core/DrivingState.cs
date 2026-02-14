using UnityEngine;

public class DrivingState : VehicleState
{
    public DrivingState(VehicleStateMachine sm, VehicleContext ctx)
        : base(sm, ctx) { }

    public override void FixedUpdate()
    {
        UpdateClutch();
        HandleGearShift();
        SimulateEngineRPM();
        ApplyEngineForces();
        ApplyEngineBraking();
        ApplySteering();
        ApplyBrakes();
        ApplyFriction();
        UpdateWheelVisuals();
        AlignToGroundNormal();
    }

    void AlignToGroundNormal()
    {
        RaycastHit hit;

        if (Physics.Raycast(context.rb.position, Vector3.down, out hit, 3f))
        {
            Quaternion targetRotation =
                Quaternion.FromToRotation(
                    context.transform.up,
                    hit.normal
                ) * context.transform.rotation;

            context.rb.MoveRotation(
                Quaternion.Slerp(
                    context.rb.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * 4f   // softer alignment
                ));
        }
    }

    void UpdateClutch()
    {
        context.clutch = context.input.Clutch;
    }

    void HandleGearShift()
    {
        if (context.clutch < 0.85f) return;

        float speed = context.rb.velocity.magnitude;

        if (context.input.ShiftUp &&
            context.currentGear < context.forwardGearRatios.Length)
            context.currentGear++;

        if (context.input.ShiftDown &&
            context.currentGear > 1)
            context.currentGear--;

        context.input.ConsumeShifts();
    }

    void SimulateEngineRPM()
    {
        float wheelRPM = GetWheelRPM();
        float ratio = Mathf.Abs(GetGearRatio());

        if (context.currentGear == 0 || context.clutch > 0.95f)
        {
            context.engineRPM = Mathf.Lerp(
                context.engineRPM,
                context.idleRPM + context.input.Throttle * (context.maxRPM - context.idleRPM),
                Time.fixedDeltaTime * 4f
            );
            return;
        }

        float targetRPM = wheelRPM * ratio * context.finalDriveRatio;

        context.engineRPM = Mathf.Lerp(
            context.engineRPM,
            Mathf.Max(context.idleRPM, targetRPM),
            Time.fixedDeltaTime * 6f
        );
    }

    void ApplyEngineForces()
    {
        if (context.currentGear == 0)
        {
            SetDriveTorque(0f);
            return;
        }

        float rpmNorm = context.engineRPM / context.maxRPM;

        // Flat torque band with gentle top-end drop
        float torqueFactor = 1f;

        if (rpmNorm < 0.2f)
            torqueFactor = Mathf.Lerp(0.6f, 1f, rpmNorm / 0.2f);
        else if (rpmNorm > 0.9f)
            torqueFactor = Mathf.Lerp(1f, 0.85f, (rpmNorm - 0.9f) / 0.1f);

        float engineTorque =
            context.maxMotorTorque *
            torqueFactor *
            context.input.Throttle;

        float rawTorque =
            engineTorque *
            GetGearRatio() *
            context.finalDriveRatio *
            (1f - context.clutch);

        float currentTorque =
            (context.colliders.RLWheel.motorTorque +
             context.colliders.RRWheel.motorTorque) * 0.5f;

        float driveTorque =
            Mathf.Lerp(currentTorque, rawTorque, Time.fixedDeltaTime * 3f);

        SetDriveTorque(driveTorque);
    }

    void ApplyEngineBraking()
    {
        if (context.currentGear <= 0 || context.clutch > 0.2f) return;

        float brake =
            context.brakeForce *
            Mathf.Abs(GetGearRatio()) *
            (context.engineRPM / context.maxRPM) *
            0.05f;   // reduced braking

        context.colliders.RLWheel.brakeTorque += brake;
        context.colliders.RRWheel.brakeTorque += brake;
    }

    float GetWheelRPM()
    {
        return Mathf.Abs(
            (context.colliders.RLWheel.rpm +
             context.colliders.RRWheel.rpm) * 0.5f
        );
    }

    float GetGearRatio()
    {
        if (context.currentGear > 0)
            return context.forwardGearRatios[context.currentGear - 1];

        return 0f;
    }

    void SetDriveTorque(float totalTorque)
    {
        context.colliders.RLWheel.motorTorque = totalTorque * 0.5f;
        context.colliders.RRWheel.motorTorque = totalTorque * 0.5f;
    }

    void ApplySteering()
    {
        float speed = context.rb.velocity.magnitude;
        float speedFactor = Mathf.Clamp01(speed / context.maxSpeed);

        float reducedAngle =
            Mathf.Lerp(context.maxSteerAngle,
                       context.maxSteerAngle * 0.5f,
                       speedFactor);

        float smoothSteer =
            Mathf.Lerp(
                context.colliders.FLWheel.steerAngle,
                context.input.Steering * reducedAngle,
                Time.fixedDeltaTime * context.steerResponse
            );

        context.colliders.FLWheel.steerAngle = smoothSteer;
        context.colliders.FRWheel.steerAngle = smoothSteer;
    }

    void ApplyBrakes()
    {
        float b = context.input.Brake * context.brakeForce;

        context.colliders.FLWheel.brakeTorque = b;
        context.colliders.FRWheel.brakeTorque = b;
        context.colliders.RLWheel.brakeTorque = b;
        context.colliders.RRWheel.brakeTorque = b;
    }

    void ApplyFriction()
    {
        float rearStiff = context.rearSideFriction;

        ApplyWheelFriction(context.colliders.RLWheel, rearStiff);
        ApplyWheelFriction(context.colliders.RRWheel, rearStiff);

        // Balanced front friction
        ApplyWheelFriction(context.colliders.FLWheel, 1.35f);
        ApplyWheelFriction(context.colliders.FRWheel, 1.35f);
    }

    void ApplyWheelFriction(WheelCollider wheel, float stiffness)
    {
        WheelFrictionCurve f = wheel.sidewaysFriction;
        f.stiffness = stiffness;
        wheel.sidewaysFriction = f;
    }

    void UpdateWheelVisuals()
    {
        UpdateWheel(context.colliders.FLWheel, context.FL_WheelMesh);
        UpdateWheel(context.colliders.FRWheel, context.FR_WheelMesh);
        UpdateWheel(context.colliders.RLWheel, context.RL_WheelMesh);
        UpdateWheel(context.colliders.RRWheel, context.RR_WheelMesh);
    }

    void UpdateWheel(WheelCollider col, Transform mesh)
    {
        col.GetWorldPose(out Vector3 p, out Quaternion r);
        mesh.position = p;
        mesh.rotation = r;
    }
}
