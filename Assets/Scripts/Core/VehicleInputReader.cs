using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleInputReader : MonoBehaviour
{
    public float Throttle { get; private set; }
    public float Steering { get; private set; }
    public float Brake { get; private set; }

    // ---- Manual gearbox inputs ----
    public bool ShiftUp { get; private set; }
    public bool ShiftDown { get; private set; }

    private VehicleInputActions inputActions;

    private void Awake()
    {
        inputActions = new VehicleInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        // -------- Throttle --------
        inputActions.Driving.Throttle.performed +=
            ctx => Throttle = ctx.ReadValue<float>();
        inputActions.Driving.Throttle.canceled +=
            _ => Throttle = 0f;

        // -------- Steering --------
        inputActions.Driving.Steering.performed +=
            ctx => Steering = ctx.ReadValue<float>();
        inputActions.Driving.Steering.canceled +=
            _ => Steering = 0f;

        // -------- Brake / Handbrake --------
        inputActions.Driving.Brake.performed +=
            ctx => Brake = ctx.ReadValue<float>();
        inputActions.Driving.Brake.canceled +=
            _ => Brake = 0f;

        // -------- Manual Gearbox --------
        inputActions.Driving.ShiftUp.performed +=
            _ => ShiftUp = true;

        inputActions.Driving.ShiftDown.performed +=
            _ => ShiftDown = true;
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    // ---- Consume methods (one press = one shift) ----
    public void ConsumeShiftUp()
    {
        ShiftUp = false;
    }

    public void ConsumeShiftDown()
    {
        ShiftDown = false;
    }
}
