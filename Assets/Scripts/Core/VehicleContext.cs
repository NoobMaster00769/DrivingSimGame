using UnityEngine;

public class VehicleContext : MonoBehaviour
{
    public Rigidbody rb;
    public VehicleInputReader input;

    [Header("Wheels")]
    public WheelColliders colliders;

    [Header("Movement")]
    public float motorForce = 1500f;
    public float maxSpeed = 25f;
    public float brakeForce = 40f;
    public float reverseForce = 20f;
    public float rollingResistance = 10f;

    [Header("Steering Settings")]
    public float maxSteerAngle = 30f;
    public float steerResponse = 5f;     // how fast wheels turn
    public float steerReturnSpeed = 7f;  // how fast wheels center

    [Header("Wheel Visuals")]
    public Transform FL_WheelMesh;
    public Transform FR_WheelMesh;
    public Transform RL_WheelMesh;
    public Transform RR_WheelMesh;

    [HideInInspector] public float throttle;
}
