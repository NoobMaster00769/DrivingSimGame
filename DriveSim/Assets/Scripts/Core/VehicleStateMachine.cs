using UnityEngine;

public class VehicleStateMachine : MonoBehaviour
{
    public VehicleState CurrentState { get; private set; }

    private VehicleContext context;

    private void Awake()
    {
        context = GetComponent<VehicleContext>();
    }

    private void Start()
    {
        ChangeState(new IdleState(this, context));
    }

    private void Update()
    {
        CurrentState?.Update();
    }

    private void FixedUpdate()
    {
        CurrentState?.FixedUpdate();
    }

    public void ChangeState(VehicleState newState)
    {
        if (CurrentState != null)
            CurrentState.Exit();

        CurrentState = newState;
        CurrentState.Enter();
    }
}
