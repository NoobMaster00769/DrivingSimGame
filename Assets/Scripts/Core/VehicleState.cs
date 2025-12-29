using UnityEngine;

public abstract class VehicleState
{
    protected VehicleStateMachine stateMachine;
    protected VehicleContext context;

    public VehicleState(VehicleStateMachine stateMachine, VehicleContext context)
    {
        this.stateMachine = stateMachine;
        this.context = context;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}
