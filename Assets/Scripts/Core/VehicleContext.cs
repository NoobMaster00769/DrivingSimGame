using UnityEngine;

public class VehicleContext : MonoBehaviour
{
    public Rigidbody rb;
    public VehicleInputReader input;

    [Header("Movement")]
    public float motorForce = 30f;
    public float maxSpeed = 25f;

    [Header("Braking")]
    public float brakeForce = 40f;
    public float reverseForce = 20f;
    public float rollingResistance = 10f;

    [HideInInspector] public float throttle;

    private void Update()
    {
        if (input != null)
            throttle = input.Throttle;
    }
}
