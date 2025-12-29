using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleInputReader : MonoBehaviour
{
    public float Throttle { get; private set; }
    public float Steering { get; private set; }
    public bool Brake { get; private set; }
    public bool Clutch { get; private set; }
    public bool ShiftUp { get; private set; }
    public bool ShiftDown { get; private set; }

    private VehicleInputActions inputActions;

    private void Awake()
    {
        inputActions = new VehicleInputActions();
    }

    private void OnEnable()
    {
        inputActions = new VehicleInputActions();
        inputActions.Enable();

        inputActions.Driving.Throttle.performed += ctx =>
        {
            Throttle = ctx.ReadValue<float>();
            Debug.Log("Throttle: " + Throttle);
        };

        inputActions.Driving.Throttle.canceled += ctx =>
        {
            Throttle = 0f;
            Debug.Log("Throttle released");
        };
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
}
