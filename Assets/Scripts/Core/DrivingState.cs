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
    }

    // ===================== CLUTCH =====================
    void UpdateClutch()
    {
        // DIGITAL clutch
        context.clutch = context.input.Clutch;
    }

    // ===================== GEARS =====================
    void HandleGearShift()
    {
        if (context.clutch < 0.85f) return;

        float speed = context.rb.velocity.magnitude;

        if (context.input.ShiftUp)
        {
            if (context.currentGear == -1 && speed < 1f)
                context.currentGear = 0;
            else if (context.currentGear == 0)
                context.currentGear = 1;
            else if (context.currentGear > 0 &&
                     context.currentGear < context.forwardGearRatios.Length)
                context.currentGear++;
        }

        if (context.input.ShiftDown)
        {
            if (context.currentGear == 1)
                context.currentGear = 0;
            else if (context.currentGear == 0 && speed < 1f)
                context.currentGear = -1;
            else if (context.currentGear > 1)
            {
                context.currentGear--;
                RevMatch();
            }
        }

        // 🔑 IMPORTANT: consume AFTER physics uses them
        context.input.ConsumeShifts();
    }


    // ===================== REV MATCH =====================
    void RevMatch()
    {
        if (context.currentGear <= 0) return;

        float wheelRPM = GetWheelRPM();
        float ratio = Mathf.Abs(GetGearRatio());

        float targetRPM =
            wheelRPM *
            ratio *
            context.finalDriveRatio;

        context.engineRPM = Mathf.Clamp(
            targetRPM,
            context.idleRPM,
            context.maxRPM
        );
    }

    // ===================== ENGINE RPM =====================
    void SimulateEngineRPM()
    {
        float wheelRPM = GetWheelRPM();
        float ratio = Mathf.Abs(GetGearRatio());

        // Neutral or clutch in → free rev
        if (context.currentGear == 0 || context.clutch > 0.95f)
        {
            context.engineRPM = Mathf.Lerp(
                context.engineRPM,
                context.idleRPM + context.input.Throttle * (context.maxRPM - context.idleRPM),
                Time.fixedDeltaTime * 5f
            );
            return;
        }

        float targetRPM = wheelRPM * ratio * context.finalDriveRatio;

        context.engineRPM = Mathf.Lerp(
            context.engineRPM,
            Mathf.Max(context.idleRPM, targetRPM),
            Time.fixedDeltaTime * 8f
        );

        // Stall
        if (context.engineRPM < context.stallRPM && context.clutch < 0.2f)
        {
            context.engineStalled = true;
            context.engineRPM = 0f;
        }
        else
        {
            context.engineStalled = false;
        }
    }

    // ===================== ENGINE FORCE =====================
    void ApplyEngineForces()
    {
        if (context.engineStalled || context.currentGear == 0)
        {
            SetDriveTorque(0f);
            return;
        }

        float rpmNorm = context.engineRPM / context.maxRPM;

        float engineTorque =
            context.maxMotorTorque *
            context.torqueCurve.Evaluate(rpmNorm) *
            context.input.Throttle;

        float rawTorque =
     engineTorque *
     GetGearRatio() *
     context.finalDriveRatio *
     (1f - context.clutch);

        float driveTorque =
            Mathf.Lerp(
                context.colliders.RLWheel.motorTorque * 2f,
                rawTorque,
                Time.fixedDeltaTime * 4f
            );


        SetDriveTorque(driveTorque);
    }

    // ===================== ENGINE BRAKING =====================
    void ApplyEngineBraking()
    {
        if (context.currentGear <= 0 || context.clutch > 0.2f) return;

        float brake =
            context.brakeForce *
            Mathf.Abs(GetGearRatio()) *
            (context.engineRPM / context.maxRPM) *
            0.1f;

        context.colliders.RLWheel.brakeTorque += brake;
        context.colliders.RRWheel.brakeTorque += brake;
    }

    // ===================== HELPERS =====================
    float GetWheelRPM()
    {
        return Mathf.Abs(
            (context.colliders.RLWheel.rpm + context.colliders.RRWheel.rpm) * 0.5f
        );
    }

    float GetGearRatio()
    {
        if (context.currentGear == -1) return context.reverseGearRatio;
        if (context.currentGear > 0) return context.forwardGearRatios[context.currentGear - 1];
        return 0f;
    }

    void SetDriveTorque(float totalTorque)
    {
        context.colliders.RLWheel.motorTorque = totalTorque * 0.5f;
        context.colliders.RRWheel.motorTorque = totalTorque * 0.5f;
    }

    // ===================== STEERING / BRAKES =====================
    void ApplySteering()
    {
        float speed = context.rb.velocity.magnitude;
        float speedFactor = Mathf.Clamp01(speed / context.maxSpeed);

        float reducedAngle =
            Mathf.Lerp(context.maxSteerAngle,
                       context.maxSteerAngle * 0.45f,
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
    public float GetNormalizedSpeed()
    {
        return Mathf.Clamp01(context.rb.velocity.magnitude / context.maxSpeed);
    }

}
