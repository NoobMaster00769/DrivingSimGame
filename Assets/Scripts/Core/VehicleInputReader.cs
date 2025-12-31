using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleInputReader : MonoBehaviour
{
    public float Throttle { get; private set; }
    public float Steering { get; private set; }
    public float Brake { get; private set; }

    private VehicleInputActions inputActions;

    private void Awake()
    {
        inputActions = new VehicleInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        // Throttle
        inputActions.Driving.Throttle.performed += ctx => Throttle = ctx.ReadValue<float>();
        inputActions.Driving.Throttle.canceled += _ => Throttle = 0f;

        // Steering
        inputActions.Driving.Steering.performed += ctx => Steering = ctx.ReadValue<float>();
        inputActions.Driving.Steering.canceled += _ => Steering = 0f;

        // Brake / Handbrake (Space)
        inputActions.Driving.Brake.performed += ctx => Brake = ctx.ReadValue<float>();
        inputActions.Driving.Brake.canceled += _ => Brake = 0f;
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
}
