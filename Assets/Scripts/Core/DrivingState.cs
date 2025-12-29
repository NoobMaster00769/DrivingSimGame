using UnityEngine;

public class DrivingState : VehicleState
{
    public DrivingState(VehicleStateMachine sm, VehicleContext ctx)
        : base(sm, ctx) { }

    public override void FixedUpdate()
    {
        Rigidbody rb = context.rb;
        if (rb == null) return;

        float throttle = context.throttle;
        Vector3 forward = context.transform.forward;

        // Get current forward speed
        float speed = Vector3.Dot(rb.velocity, forward);

        // -------- ACCELERATION --------
        if (throttle > 0.01f)
        {
            // Accelerate forward
            float accel = throttle * context.motorForce;
            speed += accel * Time.fixedDeltaTime;
        }
        // -------- BRAKING / REVERSE --------
        else if (throttle < -0.01f)
        {
            // If moving forward -> brake
            if (speed > 0.1f)
            {
                speed -= context.brakeForce * Time.fixedDeltaTime;
            }
            // If stopped or nearly stopped → reverse
            else
            {
                speed += throttle * context.reverseForce * Time.fixedDeltaTime;
            }
        }
        // -------- NATURAL ROLLING RESISTANCE --------
        else
        {
            speed = Mathf.MoveTowards(speed, 0f, context.rollingResistance * Time.fixedDeltaTime);
        }

        // Clamp final speed
        speed = Mathf.Clamp(speed, -context.maxSpeed, context.maxSpeed);

        // Apply velocity
        Vector3 newVelocity = forward * speed;
        newVelocity.y = rb.velocity.y; // keep gravity

        rb.velocity = newVelocity;
    }

    public override void Update()
    {
        float speed = Vector3.Dot(context.rb.velocity, context.transform.forward);

        // Only go idle if car is basically stopped AND no throttle input
        if (Mathf.Abs(speed) < 0.1f && Mathf.Abs(context.throttle) < 0.01f)
        {
            stateMachine.ChangeState(new IdleState(stateMachine, context));
        }
    }

}
