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

    float shiftCooldown = 0f;

    void HandleGearShift()
    {
        float speed = context.rb.velocity.magnitude;

        bool shiftUp = context.input.ShiftUp;
        bool shiftDown = context.input.ShiftDown;

        // ⛔ prevent rapid spam
        if (shiftCooldown > 0f)
        {
            shiftCooldown -= Time.fixedDeltaTime;
            context.input.ConsumeShifts();
            return;
        }

        // ---------------- SHIFT UP ----------------
        if (shiftUp)
        {
            // neutral → 1 (easy, no clutch needed)
            if (context.currentGear == 0)
            {
                context.currentGear = 1;
                shiftCooldown = 0.25f;
            }
            else if (context.clutch > 0.5f) // ✔ clutch still matters but forgiving
            {
                if (context.currentGear < context.forwardGearRatios.Length)
                {
                    int nextGear = context.currentGear + 1;

                    // 🔥 LIGHT speed gate (not restrictive)
                    float requiredSpeed =
                        nextGear * context.minSpeedForGearFactor * 0.7f;

                    if (speed > requiredSpeed)
                    {
                        context.currentGear = nextGear;
                        shiftCooldown = 0.3f; // 🔥 THIS FIXES INSTANT 1→5
                    }
                }
            }
        }

        // ---------------- SHIFT DOWN ----------------
        if (shiftDown)
        {
            if (context.currentGear > 1 && context.clutch > 0.4f)
            {
                context.currentGear--;
                shiftCooldown = 0.25f;
            }
            else if (context.currentGear == 1)
            {
                context.currentGear = 0;
                shiftCooldown = 0.2f;
            }
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

        float gearRatio = GetGearRatio();

        // smoother gear personality
        float gearBoost = Mathf.Lerp(1.2f, 0.85f, context.currentGear / 5f);

        // low gears = punchy, high gears = smoother
        float driveTorque =
            engineTorque *
            gearRatio *
            context.finalDriveRatio *
            gearBoost;

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

        // ---------------- SPEED BASED STEERING ----------------
        float reducedAngle =
            Mathf.Lerp(context.maxSteerAngle,
                       context.maxSteerAngle * 0.35f,
                       speedFactor);

        // ---------------- INPUT SMOOTHING ----------------
        float targetSteer =
            context.input.Steering * reducedAngle;

        float smoothSteer =
            Mathf.Lerp(
                context.colliders.FLWheel.steerAngle,
                targetSteer,
                Time.fixedDeltaTime * context.steerResponse);

        // ---------------- HIGH SPEED STABILITY ----------------
        Vector3 velocityDir =
            context.rb.velocity.sqrMagnitude > 1f
            ? context.rb.velocity.normalized
            : context.transform.forward;

        float alignment =
            Vector3.Dot(context.transform.forward, velocityDir);

        float stabilityAssist =
            Mathf.Clamp01(1f - alignment) * speedFactor;

        smoothSteer *= (1f - stabilityAssist * 0.5f);

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
