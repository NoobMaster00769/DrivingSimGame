using UnityEngine;

public class IdleState : VehicleState
{
    public IdleState(VehicleStateMachine sm, VehicleContext ctx) : base(sm, ctx) { }

    public override void Update()
    {
        if (Mathf.Abs(context.input.Throttle) > 0.05f)
        {
            stateMachine.ChangeState(new DrivingState(stateMachine, context));
        }
    }
}
