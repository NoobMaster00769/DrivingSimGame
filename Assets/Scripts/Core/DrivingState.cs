using UnityEngine;

public class DrivingState : VehicleState
{
    public DrivingState(VehicleStateMachine sm, VehicleContext ctx)
        : base(sm, ctx) { }

    public override void FixedUpdate()
    {
        float throttle = context.input.Throttle;
        float steering = context.input.Steering;
        float handbrake = context.input.Brake;

        // ---------------- MOTOR ----------------
        float motor = throttle * context.motorForce;
        context.colliders.RRWheel.motorTorque = motor;
        context.colliders.RLWheel.motorTorque = motor;

        // ---------------- BRAKES ----------------
        if (Mathf.Abs(throttle) < 0.05f)
            ApplyBrake(context.brakeForce);
        else
            ReleaseBrakes();

        // ---------------- STEERING ----------------
        float targetSteer = steering * context.maxSteerAngle;

        float speedFactor = Mathf.InverseLerp(0f, context.maxSpeed, context.rb.velocity.magnitude);
        float steerLimit = Mathf.Lerp(context.maxSteerAngle, context.maxSteerAngle * 0.3f, speedFactor);
        targetSteer = Mathf.Clamp(targetSteer, -steerLimit, steerLimit);

        float currentSteer = context.colliders.FLWheel.steerAngle;
        float smoothSteer = Mathf.Lerp(currentSteer, targetSteer, Time.fixedDeltaTime * context.steerResponse);

        context.colliders.FLWheel.steerAngle = smoothSteer;
        context.colliders.FRWheel.steerAngle = smoothSteer;

        // ----- BRAKE / HANDBRAKE -----
        if (context.input.Brake > 0.1f)
        {
            // Rear wheels only (drift)
            context.colliders.RRWheel.brakeTorque = context.brakeForce;
            context.colliders.RLWheel.brakeTorque = context.brakeForce;

            // Front wheels free to steer
            context.colliders.FRWheel.brakeTorque = 0f;
            context.colliders.FLWheel.brakeTorque = 0f;
        }
        else
        {
            ReleaseBrakes();
        }


        // ---------------- WEIGHT TRANSFER ----------------
        ApplyWeightTransfer();

        // ---------------- VISUALS ----------------
        UpdateWheelVisuals();
    }

    void ApplyBrake(float force)
    {
        context.colliders.FRWheel.brakeTorque = force;
        context.colliders.FLWheel.brakeTorque = force;
        context.colliders.RRWheel.brakeTorque = force;
        context.colliders.RLWheel.brakeTorque = force;
    }

    void ReleaseBrakes()
    {
        context.colliders.FRWheel.brakeTorque = 0;
        context.colliders.FLWheel.brakeTorque = 0;
        context.colliders.RRWheel.brakeTorque = 0;
        context.colliders.RLWheel.brakeTorque = 0;
    }

    void ApplyHandbrake(bool active)
    {
        WheelFrictionCurve sideFriction;

        if (active)
        {
            sideFriction = context.colliders.RRWheel.sidewaysFriction;
            sideFriction.stiffness = 0.4f;
            context.colliders.RRWheel.sidewaysFriction = sideFriction;
            context.colliders.RLWheel.sidewaysFriction = sideFriction;

            context.colliders.RRWheel.brakeTorque = context.brakeForce * 2f;
            context.colliders.RLWheel.brakeTorque = context.brakeForce * 2f;
        }
        else
        {
            sideFriction = context.colliders.RRWheel.sidewaysFriction;
            sideFriction.stiffness = 1.5f;
            context.colliders.RRWheel.sidewaysFriction = sideFriction;
            context.colliders.RLWheel.sidewaysFriction = sideFriction;
        }
    }

    void ApplyWeightTransfer()
    {
        Vector3 localVel = context.transform.InverseTransformDirection(context.rb.velocity);
        float zShift = Mathf.Clamp(localVel.z * 0.02f, -0.25f, 0.25f);
        context.rb.centerOfMass = new Vector3(0f, -0.4f, -zShift);
    }

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
