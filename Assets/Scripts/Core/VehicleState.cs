using UnityEngine;

public abstract class VehicleState
{
    protected VehicleStateMachine stateMachine;
    protected VehicleContext context;

    protected VehicleState(VehicleStateMachine sm, VehicleContext ctx)
    {
        stateMachine = sm;
        context = ctx;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}
