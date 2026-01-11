using UnityEngine;

public class VehicleContext : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public VehicleInputReader input;

    [Header("Wheel Colliders")]
    public WheelColliders colliders;

    [Header("Wheel Meshes")]
    public Transform FL_WheelMesh;
    public Transform FR_WheelMesh;
    public Transform RL_WheelMesh;
    public Transform RR_WheelMesh;

    [Header("Engine")]
    public float maxMotorTorque = 1500f;
    public float maxRPM = 6000f;
    public float idleRPM = 900f;

    [Tooltip("X = normalized RPM (0–1), Y = torque multiplier")]
    public AnimationCurve torqueCurve;

    [Header("Brakes")]
    public float brakeForce = 3000f;

    [Header("Steering")]
    public float maxSteerAngle = 30f;
    public float steerResponse = 6f;

    [Header("Friction")]
    public float rearSideFriction = 1.4f;
    public float rearHandbrakeFriction = 0.6f;

    [Header("Speed")]
    public float maxSpeed = 40f; // m/s

    [HideInInspector] public float engineRPM;

    private void Start()
    {
        rb.centerOfMass = new Vector3(0f, -0.45f, 0f);
    }
}
