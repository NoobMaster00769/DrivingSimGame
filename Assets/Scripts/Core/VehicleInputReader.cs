using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleInputReader : MonoBehaviour
{

    public float Throttle { get; private set; }
    public float Steering { get; private set; }
    public float Brake { get; private set; }

    // DIGITAL CLUTCH (keyboard)
    public float Clutch { get; private set; }

    // One-frame shift flags
    public bool ShiftUp { get; private set; }
    public bool ShiftDown { get; private set; }
    public bool ResetCar { get; private set; }

    private VehicleInputActions input;

    void Awake()
    {
        input = new VehicleInputActions();

        var json = PlayerPrefs.GetString("rebinds", "");
        if (!string.IsNullOrEmpty(json))
            input.asset.LoadBindingOverridesFromJson(json);

        // Throttle
        input.Driving.Throttle.performed += ctx => Throttle = ctx.ReadValue<float>();
        input.Driving.Throttle.canceled += _ => Throttle = 0f;

        // Steering
        input.Driving.Steering.performed += ctx => Steering = ctx.ReadValue<float>();
        input.Driving.Steering.canceled += _ => Steering = 0f;

        // Brake
        input.Driving.Brake.performed += ctx => Brake = ctx.ReadValue<float>();
        input.Driving.Brake.canceled += _ => Brake = 0f;

        // Clutch (Left Shift)
        input.Driving.Clutch.performed += _ => Clutch = 1f;
        input.Driving.Clutch.canceled += _ => Clutch = 0f;

        // Gear shifts
        input.Driving.ShiftUp.performed += _ => ShiftUp = true;
        input.Driving.ShiftDown.performed += _ => ShiftDown = true;

        // Reset Car
        input.Driving.ResetCar.performed += _ => ResetCar = true;
    }

    void OnEnable()
    {
        input.Enable();
    }

    void OnDisable()
    {
        input.Disable();
    }

    // CALLED MANUALLY FROM DrivingState
    public void ConsumeShifts()
    {
        ShiftUp = false;
        ShiftDown = false;
    }

    public void ConsumeReset()
    {
        ResetCar = false;
    }

    public VehicleInputActions GetActions()
    {
        return input;
    }

}
