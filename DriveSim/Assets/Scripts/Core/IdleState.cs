using UnityEngine;

public class IdleState : VehicleState
{
    public IdleState(VehicleStateMachine sm, VehicleContext ctx)
        : base(sm, ctx) { }

    public override void Enter()
    {
        Debug.Log("Entered Idle State");
    }

    public override void Update()
    {
        // Enter driving state if player presses forward OR reverse
        if (Mathf.Abs(context.throttle) > 0.01f)
        {
            Debug.Log("Switching to Driving State");
            stateMachine.ChangeState(new DrivingState(stateMachine, context));
        }
    }
}
