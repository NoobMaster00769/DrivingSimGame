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
        ApplyDriveTorque();
        ApplySteering();
        ApplyBrakes();
        ApplyFriction();
        UpdateWheelVisuals();
    }

    // ---------------- CLUTCH ----------------
    void UpdateClutch()
    {
        context.clutch = Mathf.Lerp(
            context.clutch,
            context.input.Clutch,
            Time.fixedDeltaTime * context.clutchEngageSpeed
        );
    }

    // ---------------- GEARS ----------------
    void HandleGearShift()
    {
        // MUST press clutch to shift
        if (context.clutch < 0.8f)
            return;

        float speed = context.rb.velocity.magnitude;

        if (context.input.ShiftUp)
        {
            // Reverse → Neutral
            if (context.currentGear == -1 && speed < 1f)
                context.currentGear = 0;

            // Neutral → 1st
            else if (context.currentGear == 0)
                context.currentGear = 1;

            // Forward gears
            else if (context.currentGear > 0 &&
                     context.currentGear < context.forwardGearRatios.Length)
                context.currentGear++;

            context.input.ConsumeShiftUp();
        }

        if (context.input.ShiftDown)
        {
            // Forward → Neutral
            if (context.currentGear == 1)
                context.currentGear = 0;

            // Neutral → Reverse
            else if (context.currentGear == 0 && speed < 1f)
                context.currentGear = -1;

            // Forward gears
            else if (context.currentGear > 1)
            {
                context.currentGear--;
                RevMatch(); // 👈 real rev-matching
            }

            context.input.ConsumeShiftDown();
        }
    }

    // ---------------- REV MATCH ----------------
    void RevMatch()
    {
        float wheelRPM =
            (context.colliders.RRWheel.rpm + context.colliders.RLWheel.rpm) * 0.5f;

        float targetRPM =
            Mathf.Abs(wheelRPM) *
            Mathf.Abs(GetCurrentGearRatio()) *
            context.finalDriveRatio;

        context.engineRPM = Mathf.Clamp(
            targetRPM,
            context.idleRPM,
            context.maxRPM
        );
    }

    // ---------------- ENGINE RPM ----------------
    void SimulateEngineRPM()
    {
        float wheelRPM =
            (context.colliders.RRWheel.rpm + context.colliders.RLWheel.rpm) * 0.5f;

        float gearRatio = GetCurrentGearRatio();

        float targetRPM =
            Mathf.Abs(wheelRPM) *
            Mathf.Abs(gearRatio) *
            context.finalDriveRatio;

        // Neutral or clutch pressed → free rev
        if (context.currentGear == 0 || context.clutch > 0.9f)
        {
            context.engineRPM = Mathf.Lerp(
                context.engineRPM,
                context.idleRPM + context.input.Throttle * (context.maxRPM - context.idleRPM),
                Time.fixedDeltaTime * 5f
            );
        }
        else
        {
            context.engineRPM = Mathf.Lerp(
                context.engineRPM,
                Mathf.Max(context.idleRPM, targetRPM),
                (1f - context.clutch) * Time.fixedDeltaTime * 6f
            );
        }

        // Stall
        if (context.engineRPM < context.stallRPM &&
            context.currentGear != 0 &&
            context.clutch < 0.2f)
        {
            context.engineStalled = true;
            context.engineRPM = 0f;
        }
        else
        {
            context.engineStalled = false;
        }

        context.engineRPM = Mathf.Clamp(context.engineRPM, 0f, context.maxRPM);
    }

    // ---------------- TORQUE ----------------
    void ApplyDriveTorque()
    {
        if (context.engineStalled || context.currentGear == 0)
        {
            context.colliders.RRWheel.motorTorque = 0f;
            context.colliders.RLWheel.motorTorque = 0f;
            return;
        }

        // Prevent wrong-direction torque
        float forwardVel = Vector3.Dot(context.rb.velocity, context.transform.forward);
        float gearRatio = GetCurrentGearRatio();

        if (gearRatio < 0 && forwardVel > 0.5f) return;
        if (gearRatio > 0 && forwardVel < -0.5f) return;

        float rpmNorm = context.engineRPM / context.maxRPM;
        float engineTorque =
            context.maxMotorTorque *
            context.torqueCurve.Evaluate(rpmNorm) *
            context.input.Throttle;

        float driveTorque =
            engineTorque *
            gearRatio *
            context.finalDriveRatio *
            (1f - context.clutch);

        context.colliders.RRWheel.motorTorque = driveTorque;
        context.colliders.RLWheel.motorTorque = driveTorque;
    }

    float GetCurrentGearRatio()
    {
        if (context.currentGear == -1)
            return context.reverseGearRatio;

        if (context.currentGear > 0)
            return context.forwardGearRatios[context.currentGear - 1];

        return 0f;
    }

    // ---------------- STEERING ----------------
    void ApplySteering()
    {
        float speed = context.rb.velocity.magnitude;
        float target = context.input.Steering *
                       Mathf.Lerp(context.maxSteerAngle, context.maxSteerAngle * 0.35f, speed / context.maxSpeed);

        context.colliders.FLWheel.steerAngle =
            Mathf.Lerp(context.colliders.FLWheel.steerAngle, target, Time.fixedDeltaTime * context.steerResponse);

        context.colliders.FRWheel.steerAngle = context.colliders.FLWheel.steerAngle;
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
        bool hb = context.input.Brake > 0.1f;
        var f = context.colliders.RRWheel.sidewaysFriction;
        f.stiffness = hb ? context.rearHandbrakeFriction : context.rearSideFriction;
        context.colliders.RRWheel.sidewaysFriction = f;
        context.colliders.RLWheel.sidewaysFriction = f;
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
