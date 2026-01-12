using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleInputReader : MonoBehaviour
{
    public float Throttle { get; private set; }
    public float Steering { get; private set; }
    public float Brake { get; private set; }
    public float Clutch { get; private set; }

    public bool ShiftUp { get; private set; }
    public bool ShiftDown { get; private set; }

    private VehicleInputActions actions;

    private void Awake()
    {
        actions = new VehicleInputActions();
    }

    private void OnEnable()
    {
        actions.Enable();

        actions.Driving.Throttle.performed += c => Throttle = c.ReadValue<float>();
        actions.Driving.Throttle.canceled += _ => Throttle = 0f;

        actions.Driving.Steering.performed += c => Steering = c.ReadValue<float>();
        actions.Driving.Steering.canceled += _ => Steering = 0f;

        actions.Driving.Brake.performed += c => Brake = c.ReadValue<float>();
        actions.Driving.Brake.canceled += _ => Brake = 0f;

        actions.Driving.Clutch.performed += _ => Clutch = 1f;
        actions.Driving.Clutch.canceled += _ => Clutch = 0f;

        actions.Driving.ShiftUp.performed += _ => ShiftUp = true;
        actions.Driving.ShiftDown.performed += _ => ShiftDown = true;
    }

    public void ConsumeShiftUp() => ShiftUp = false;
    public void ConsumeShiftDown() => ShiftDown = false;

    private void OnDisable()
    {
        actions.Disable();
    }
}
