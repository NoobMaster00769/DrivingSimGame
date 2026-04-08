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



    void UpdateClutch()
    {
        context.clutch = context.input.Clutch;
    }



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


        float[] gearMinSpeed = {
        0f,    // neutral
        0f,    // 1st
        10f,   // 2nd  
        20f,   // 3rd  
        32f,   // 4th  
        42f,   // 5th  
    };

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

        if (shiftDown)
        {
            if (context.currentGear > 1)
            {
                if (speed < gearMinSpeed[context.currentGear] + 2f)
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



    void ApplyDrive()
    {
        if (context.currentGear == 0)
        {
            SetDriveTorque(0f);
            context.engineRPM = Mathf.Lerp(
                context.engineRPM,
                context.idleRPM + context.input.Throttle * (context.maxRPM - context.idleRPM),
                Time.fixedDeltaTime * 6f);
            return;
        }

        if (context.clutch > 0.5f)
        {
            SetDriveTorque(0f);
            context.engineRPM = Mathf.Lerp(
                context.engineRPM,
                context.idleRPM + context.input.Throttle * (context.maxRPM - context.idleRPM),
                Time.fixedDeltaTime * 5f);
            return;
        }

        if (context.currentGear < 0)
        {
            SetDriveTorque(context.maxMotorTorque * 0.6f * context.input.Throttle);
            return;
        }

        float speed = context.rb.velocity.magnitude;
        float rpmNorm = context.engineRPM / context.maxRPM;


        float redlineResistance = 1f;
        if (rpmNorm > 0.92f)
            redlineResistance = Mathf.Lerp(1f, 0f, (rpmNorm - 0.92f) / 0.08f);


        float minSpeed = GetMinSpeedForGear(context.currentGear);
        float efficiency = 1f;
        if (speed < minSpeed)
        {
            float t = speed / Mathf.Max(0.1f, minSpeed);
            efficiency = Mathf.Lerp(0.45f, 1f, t * t);
        }

        float curveFactor = context.torqueCurve != null
            ? context.torqueCurve.Evaluate(rpmNorm)
            : 1f;

        float engineTorque =
            context.maxMotorTorque *
            context.input.Throttle *
            redlineResistance *
            efficiency *
            curveFactor;

        float gearRatio = GetGearRatio();


        float gearBoost = Mathf.Lerp(1.6f, 0.85f, (context.currentGear - 1f) / 4f);

        float driveTorque = engineTorque * gearRatio * context.finalDriveRatio * gearBoost;



        // Kickstart
        if (speed < 1.5f && context.input.Throttle > 0.2f)
            driveTorque += 600f;


        float speedCapFactor = 1f - Mathf.Clamp01(
            (speed - context.maxSpeed * 0.97f) / (context.maxSpeed * 0.05f));
        driveTorque *= speedCapFactor;

        SetDriveTorque(driveTorque);

        if (efficiency < 0.25f && context.input.Throttle > 0.5f)
            context.engineRPM -= 2000f * Time.fixedDeltaTime;
    }

    float GetMinSpeedForGear(int gear)
    {

        switch (gear)
        {
            case 1: return 0f;
            case 2: return 8f;
            case 3: return 17f;
            case 4: return 27f;
            case 5: return 38f;
            default: return 0f;
        }
    }



    void ApplyEngineBraking()
    {
        if (context.currentGear <= 0) return;
        if (context.input.Throttle > 0.1f) return;

        float brake = context.brakeForce * 0.04f * (context.engineRPM / context.maxRPM);
        context.colliders.RLWheel.brakeTorque += brake;
        context.colliders.RRWheel.brakeTorque += brake;
    }



    float currentSteer;
    float steerVelocity;

    void ApplySteering()
    {
        float speed = context.rb.velocity.magnitude;
        float speed01 = Mathf.Clamp01(speed / context.maxSpeed);

        float input = context.input.Steering;

        float maxAngle = Mathf.Lerp(
            context.maxSteerAngle,
            context.maxSteerAngle * 0.65f,
            speed01);


        float buildRate = Mathf.Lerp(42f, 9f, speed01 * speed01);  
        float returnRate = Mathf.Lerp(80f, 160f, speed01);           
        if (Mathf.Abs(input) > 0.05f)
        {
            float targetSteer = input * maxAngle;
            float delta = targetSteer - currentSteer;
            float step = buildRate * Time.fixedDeltaTime;
            currentSteer += Mathf.Clamp(delta, -step, step);
        }
        else
        {
            float step = returnRate * Time.fixedDeltaTime;
            currentSteer = Mathf.MoveTowards(currentSteer, 0f, step);
        }

        currentSteer = Mathf.Clamp(currentSteer, -maxAngle, maxAngle);

        context.colliders.FLWheel.steerAngle = currentSteer;
        context.colliders.FRWheel.steerAngle = currentSteer;
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
        float speed = context.rb.velocity.magnitude;
        float speed01 = Mathf.Clamp01(speed / context.maxSpeed);

        float frontGrip = Mathf.Lerp(1.3f, 1.7f, speed01);

        float rearGrip = context.rearSideFriction;

        ApplyWheelFriction(context.colliders.FLWheel, frontGrip);
        ApplyWheelFriction(context.colliders.FRWheel, frontGrip);
        ApplyWheelFriction(context.colliders.RLWheel, rearGrip);
        ApplyWheelFriction(context.colliders.RRWheel, rearGrip);

        StabilizeLateralVelocity(speed01);
    }

    void StabilizeLateralVelocity(float speed01)
    {
        Vector3 velocity = context.rb.velocity;
        Vector3 forward = context.transform.forward;
        Vector3 right = context.transform.right;

        float forwardVel = Vector3.Dot(velocity, forward);
        float lateralVel = Vector3.Dot(velocity, right);


        float stability = Mathf.Lerp(0.35f, 0.62f, speed01);

        Vector3 corrected =
            forward * forwardVel +
            right * (lateralVel * (1f - stability));

        context.rb.velocity = Vector3.Lerp(
            context.rb.velocity,
            corrected,
            Time.fixedDeltaTime * 5f);
    }

    void ApplyWheelFriction(WheelCollider wheel, float stiffness)
    {
        WheelFrictionCurve f = wheel.sidewaysFriction;
        f.extremumSlip = 0.22f;
        f.extremumValue = 1.1f;
        f.asymptoteSlip = 0.45f;
        f.asymptoteValue = 0.85f;
        f.stiffness = stiffness;
        wheel.sidewaysFriction = f;
    }



    void ApplyDownforce()
    {
        float speed = context.rb.velocity.magnitude;
        float downforce = speed * speed * 0.25f;  // was 0.4 — less drag at top end
        context.rb.AddForce(-context.transform.up * downforce, ForceMode.Force);
    }



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