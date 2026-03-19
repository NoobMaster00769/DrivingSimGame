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
        ApplyDrive();
        ApplyEngineBraking();
        ApplySteering();
        ApplyBrakes();
        ApplyFriction();
        UpdateWheelVisuals();
        AlignToGroundNormal();
    }

    void AlignToGroundNormal()
    {
        if (Physics.Raycast(context.rb.position, Vector3.down, out RaycastHit hit, 3f))
        {
            Quaternion targetRotation =
                Quaternion.FromToRotation(context.transform.up, hit.normal)
                * context.transform.rotation;

            context.rb.MoveRotation(
                Quaternion.Slerp(
                    context.rb.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * 4f));
        }
    }

    // ==============================
    // CLUTCH
    // ==============================

    void UpdateClutch()
    {
        context.clutch = context.input.Clutch;
    }

    // ==============================
    // GEAR SHIFT
    // ==============================

    void HandleGearShift()
    {
        if (context.clutch < 0.85f) return;

        float speed = context.rb.velocity.magnitude;

        // SHIFT UP (soft gating)
        if (context.input.ShiftUp &&
            context.currentGear < context.forwardGearRatios.Length)
        {
            float minSpeed =
                context.currentGear * context.minSpeedForGearFactor;

            if (speed > minSpeed * 0.6f) // soft allowance
            {
                context.currentGear++;
            }
        }

        // SHIFT DOWN
        if (context.input.ShiftDown &&
            context.currentGear > 0)
        {
            context.currentGear--;
        }

        context.input.ConsumeShifts();
    }

    // ==============================
    // RPM SIMULATION (Mechanical)
    // ==============================

    void SimulateEngineRPM()
    {
        // Neutral
        if (context.currentGear == 0)
        {
            context.engineRPM = Mathf.Lerp(
                context.engineRPM,
                context.idleRPM +
                context.input.Throttle *
                (context.maxRPM - context.idleRPM),
                Time.fixedDeltaTime * 6f);

            return;
        }

        float wheelRPM = GetWheelRPM();
        float gearRatio = Mathf.Abs(GetGearRatio());

        float targetRPM =
            wheelRPM *
            gearRatio *
            context.finalDriveRatio;

        context.engineRPM = Mathf.Clamp(
            targetRPM,
            context.idleRPM,
            context.maxRPM);
    }

    // ==============================
    // DRIVE (FUN + CLEAN)
    // ==============================

    void ApplyDrive()
    {
        // Neutral
        if (context.currentGear == 0)
        {
            SetDriveTorque(0f);
            return;
        }

        // ---------------- REVERSE (independent) ----------------
        if (context.currentGear < 0)
        {
            float reverseTorque =
                context.maxMotorTorque * 0.6f * context.input.Throttle;

            SetDriveTorque(reverseTorque);
            return;
        }

        float speed = context.rb.velocity.magnitude;
        float rpmNorm = context.engineRPM / context.maxRPM;

        // ---------------- REDLINE RESISTANCE ----------------
        float redlineResistance = 1f;

        if (rpmNorm > 0.92f)
        {
            float over = (rpmNorm - 0.92f) / 0.08f;
            redlineResistance = Mathf.Lerp(1f, 0f, over);
        }

        // ---------------- LUGGING (KEY FIX) ----------------
        float minSpeedForGear =
            context.currentGear * context.minSpeedForGearFactor;

        float efficiency = 1f;

        if (speed < minSpeedForGear)
        {
            float t = speed / Mathf.Max(0.1f, minSpeedForGear);

            // NON-LINEAR DROP (this is the key)
            t = t * t * t; // cubic falloff

            efficiency = Mathf.Lerp(0.02f, 1f, t);
        }

        // ---------------- ENGINE TORQUE ----------------
        float engineTorque =
            context.maxMotorTorque *
            context.input.Throttle *
            redlineResistance *
            efficiency;

        float driveTorque =
            engineTorque *
            GetGearRatio() *
            context.finalDriveRatio;

        SetDriveTorque(driveTorque);

        // ---------------- ENGINE STRUGGLE FEEL ----------------
        if (efficiency < 0.25f && context.input.Throttle > 0.5f)
        {
            context.engineRPM -= 2000f * Time.fixedDeltaTime;
        }
    }

    // ==============================
    // ENGINE BRAKING
    // ==============================

    void ApplyEngineBraking()
    {
        if (context.currentGear <= 0) return;

        if (context.input.Throttle > 0.1f) return;

        float brake =
            context.brakeForce *
            0.05f *
            (context.engineRPM / context.maxRPM);

        context.colliders.RLWheel.brakeTorque += brake;
        context.colliders.RRWheel.brakeTorque += brake;
    }

    // ==============================
    // HELPERS
    // ==============================

    float GetWheelRPM()
    {
        return Mathf.Abs(
            (context.colliders.RLWheel.rpm +
             context.colliders.RRWheel.rpm) * 0.5f);
    }

    float GetGearRatio()
    {
        if (context.currentGear > 0)
            return context.forwardGearRatios[context.currentGear - 1];

        return context.reverseGearRatio;
    }

    void SetDriveTorque(float totalTorque)
    {
        context.colliders.RLWheel.motorTorque = totalTorque * 0.5f;
        context.colliders.RRWheel.motorTorque = totalTorque * 0.5f;
    }

    // ==============================
    // STEERING
    // ==============================

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
                Time.fixedDeltaTime * context.steerResponse);

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
        ApplyWheelFriction(context.colliders.RLWheel, context.rearSideFriction);
        ApplyWheelFriction(context.colliders.RRWheel, context.rearSideFriction);
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
