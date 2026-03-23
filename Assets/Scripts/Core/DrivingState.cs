using UnityEngine;
using System.Collections;

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
        ApplyDownforce();
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

    // ══════════════════════════════════════════════════════════════
    //  CLUTCH
    // ══════════════════════════════════════════════════════════════

    void UpdateClutch()
    {
        context.clutch = context.input.Clutch;
    }

    // ══════════════════════════════════════════════════════════════
    //  GEAR SHIFT
    // ══════════════════════════════════════════════════════════════

    float shiftCooldown = 0f;

    void HandleGearShift()
    {
        float speed = context.rb.velocity.magnitude;

        bool shiftUp = context.input.ShiftUp;
        bool shiftDown = context.input.ShiftDown;

        if (shiftCooldown > 0f)
        {
            shiftCooldown -= Time.fixedDeltaTime;
            context.input.ConsumeShifts();
            return;
        }

        // Min speed thresholds scaled to actual maxSpeed (28 m/s) not the old 85
        // These feel natural — you can get into 3rd at city speeds
        float[] gearMinSpeed = {
    0f,   // neutral
    0f,   // 1st
    7f,   // 2nd
    14f,  // 3rd
    22f,  // 4th
    30f   // 5th
};

        // SHIFT UP
        if (shiftUp)
        {
            if (context.currentGear == 0)
            {
                context.currentGear = 1;
                shiftCooldown = 0.25f;
            }
            else if (context.clutch > 0.5f)
            {
                if (context.currentGear < context.forwardGearRatios.Length)
                {
                    int nextGear = context.currentGear + 1;
                    if (speed > gearMinSpeed[nextGear])
                    {
                        context.currentGear = nextGear;
                        shiftCooldown = 0.35f;
                    }
                }
            }
        }

        // SHIFT DOWN
        if (shiftDown)
        {
            if (context.currentGear > 1)
            {
                if (speed < gearMinSpeed[context.currentGear])
                {
                    context.currentGear--;
                    shiftCooldown = 0.25f;
                }
            }
            else if (context.currentGear == 1)
            {
                context.currentGear = 0;
                shiftCooldown = 0.2f;
            }
        }

        context.input.ConsumeShifts();
    }

    // ══════════════════════════════════════════════════════════════
    //  RPM
    // ══════════════════════════════════════════════════════════════

    void SimulateEngineRPM()
    {
        if (context.currentGear == 0)
        {
            context.engineRPM = Mathf.Lerp(
                context.engineRPM,
                context.idleRPM + context.input.Throttle * (context.maxRPM - context.idleRPM),
                Time.fixedDeltaTime * 6f);
            return;
        }

        float wheelRPM = GetWheelRPM();
        float gearRatio = Mathf.Abs(GetGearRatio());
        float targetRPM = wheelRPM * gearRatio * context.finalDriveRatio;

        context.engineRPM = Mathf.Clamp(targetRPM, context.idleRPM, context.maxRPM);
    }

    // ══════════════════════════════════════════════════════════════
    //  DRIVE
    // ══════════════════════════════════════════════════════════════

    void ApplyDrive()
    {
        if (context.currentGear == 0)
        {
            SetDriveTorque(0f);
            return;
        }

        if (context.currentGear < 0)
        {
            SetDriveTorque(context.maxMotorTorque * 0.6f * context.input.Throttle);
            return;
        }

        float speed = context.rb.velocity.magnitude;
        float rpmNorm = context.engineRPM / context.maxRPM;

        // Redline resistance
        float redlineResistance = 1f;
        if (rpmNorm > 0.92f)
        {
            float over = (rpmNorm - 0.92f) / 0.08f;
            redlineResistance = Mathf.Lerp(1f, 0f, over);
        }

        // Lugging — scaled to actual max speed so every gear is usable
        // minSpeedForGear is now per-gear, not a linear factor
        float minSpeedForGear = GetMinSpeedForGear(context.currentGear);
        float efficiency = 1f;
        if (speed < minSpeedForGear)
        {
            float t = speed / Mathf.Max(0.1f, minSpeedForGear);
            efficiency = Mathf.Lerp(0.4f, 1f, t * t);
        }

        float engineTorque =
            context.maxMotorTorque *
            context.input.Throttle *
            redlineResistance *
            efficiency;

        float gearRatio = GetGearRatio();
        float gearBoost = Mathf.Lerp(1.8f, 0.6f, (context.currentGear - 1f) / 4f);

        float driveTorque =
            engineTorque *
            gearRatio *
            context.finalDriveRatio *
            gearBoost;

        // High gear low speed penalty — but scaled to actual max, not 25
        if (context.currentGear >= 4 && speed < context.maxSpeed * 0.6f)
            driveTorque *= 0.5f;

        // Kickstart
        if (speed < 1.5f && context.input.Throttle > 0.2f)
            driveTorque += 800f;

        SetDriveTorque(driveTorque);

        if (efficiency < 0.25f && context.input.Throttle > 0.5f)
            context.engineRPM -= 2000f * Time.fixedDeltaTime;
    }

    // Per-gear minimum speed — fixed values that work within the 28 m/s cap
    float GetMinSpeedForGear(int gear)
    {
        switch (gear)
        {
            case 1: return 0f;
            case 2: return 5f;
            case 3: return 11f;
            case 4: return 18f;
            case 5: return 26f;
            default: return 0f;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  ENGINE BRAKING
    // ══════════════════════════════════════════════════════════════

    void ApplyEngineBraking()
    {
        if (context.currentGear <= 0) return;
        if (context.input.Throttle > 0.1f) return;

        float brake =
            context.brakeForce * 0.05f *
            (context.engineRPM / context.maxRPM);

        context.colliders.RLWheel.brakeTorque += brake;
        context.colliders.RRWheel.brakeTorque += brake;
    }

    // ══════════════════════════════════════════════════════════════
    //  STEERING  — the core feel fix
    //
    //  Problems with the old system:
    //  1. speed01 = speed / maxSpeed, but maxSpeed in context was 85 while
    //     the car never exceeds 28 — so speed01 was always ~0.33, meaning
    //     steering was always in "slow heavy" mode and never lightened up.
    //  2. StabilizeLateralVelocity with stability=0.55 completely cancelled
    //     the natural cornering arc — the car felt like it was on rails but
    //     also somehow didn't turn properly.
    //  3. No progressive feel — turning is the same whether you're crawling
    //     or at full speed.
    //
    //  Fix:
    //  - speed01 now uses actual max (28) so the full steering range is used
    //  - Self-centering is stronger at speed (car stays composed on straights)
    //  - Lateral resistance is gentler — lets the car arc naturally
    //  - StabilizeLateralVelocity stability reduced from 0.55 → 0.25
    //    so the car has genuine body roll feel through corners
    // ══════════════════════════════════════════════════════════════

    float currentSteer;
    float steerVelocity;

    void ApplySteering()
    {
        float speed = context.rb.velocity.magnitude;
        float speed01 = Mathf.Clamp01(speed / context.maxSpeed);

        float input = context.input.Steering;

        // steering range reduces at speed (real car feel)
        float maxAngle = Mathf.Lerp(
            context.maxSteerAngle,
            context.maxSteerAngle * 0.5f,
            speed01);

        float targetSteer = input * maxAngle;

        // slower response at speed → stable
        float response = Mathf.Lerp(11f, 4f, speed01);

        currentSteer = Mathf.Lerp(
            currentSteer,
            targetSteer,
            Time.fixedDeltaTime * response);

        // strong self-centering at speed
        if (Mathf.Abs(input) < 0.05f)
        {
            float centerSpeed = Mathf.Lerp(4f, 10f, speed01);
            currentSteer = Mathf.Lerp(currentSteer, 0f, Time.fixedDeltaTime * centerSpeed);
        }

        // resistance when sliding (adds weight feel)
        float lateral = Vector3.Dot(context.rb.velocity, context.transform.right);
        float resistance = Mathf.Clamp01(Mathf.Abs(lateral) / 10f);
        currentSteer *= Mathf.Lerp(1f, 0.75f, resistance);

        context.colliders.FLWheel.steerAngle = currentSteer;
        context.colliders.FRWheel.steerAngle = currentSteer;
    }
    // ══════════════════════════════════════════════════════════════
    //  BRAKES
    // ══════════════════════════════════════════════════════════════

    void ApplyBrakes()
    {
        float b = context.input.Brake * context.brakeForce;
        context.colliders.FLWheel.brakeTorque = b;
        context.colliders.FRWheel.brakeTorque = b;
        context.colliders.RLWheel.brakeTorque = b;
        context.colliders.RRWheel.brakeTorque = b;
    }

    // ══════════════════════════════════════════════════════════════
    //  FRICTION
    // ══════════════════════════════════════════════════════════════

    void ApplyFriction()
    {
        float speed = context.rb.velocity.magnitude;
        float speed01 = Mathf.Clamp01(speed / context.maxSpeed);

        // MUCH stronger grip
        float frontGrip = Mathf.Lerp(1.7f, 2.3f, speed01);
        float rearGrip = Mathf.Lerp(1.6f, 2.1f, speed01);

        ApplyWheelFriction(context.colliders.FLWheel, frontGrip);
        ApplyWheelFriction(context.colliders.FRWheel, frontGrip);
        ApplyWheelFriction(context.colliders.RLWheel, rearGrip);
        ApplyWheelFriction(context.colliders.RRWheel, rearGrip);

        StabilizeLateralVelocity();
    }

    void StabilizeLateralVelocity()
    {
        Vector3 velocity = context.rb.velocity;
        Vector3 forward = context.transform.forward;
        Vector3 right = context.transform.right;

        float forwardVel = Vector3.Dot(velocity, forward);
        float lateralVel = Vector3.Dot(velocity, right);

        // 0.25 instead of 0.55 — car now arcs through corners properly.
        // At 0.55 the lateral velocity was being cancelled so aggressively
        // that turning felt numb even though the wheels were turning.
        float stability = 0.32f;

        Vector3 corrected =
            forward * forwardVel +
            right * (lateralVel * (1f - stability));

        context.rb.velocity = Vector3.Lerp(
            context.rb.velocity,
            corrected,
            Time.fixedDeltaTime * 3f);
    }

    void ApplyWheelFriction(WheelCollider wheel, float stiffness)
    {
        WheelFrictionCurve f = wheel.sidewaysFriction;
        f.extremumSlip = 0.25f;
        f.extremumValue = 1.2f;
        f.asymptoteSlip = 0.5f;
        f.asymptoteValue = 0.9f;
        f.stiffness = stiffness;
        wheel.sidewaysFriction = f;
    }

    // ══════════════════════════════════════════════════════════════
    //  DOWNFORCE
    // ══════════════════════════════════════════════════════════════

    void ApplyDownforce()
    {
        float speed = context.rb.velocity.magnitude;
        float downforce = speed * speed * 0.45f;
        context.rb.AddForce(-context.transform.up * downforce, ForceMode.Force);
    }

    // ══════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════

    float GetWheelRPM()
    {
        return Mathf.Abs(
            (context.colliders.RLWheel.rpm + context.colliders.RRWheel.rpm) * 0.5f);
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