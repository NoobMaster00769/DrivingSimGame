using UnityEngine;

public class VehicleContext : MonoBehaviour
{
    [Header("Differential (LSD)")]
    [Range(0f, 1f)]
    public float lsdStrength = 0.4f;   // reduced for smoother exits
    public float maxLsdBias = 2.0f;    // less snap torque bias

    [Header("Traction Control")]
    public bool tractionControlEnabled = false; // disable for smooth flow
    public float slipThreshold = 0.2f;
    public float tcStrength = 4f;

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

    // ---------------- ENGINE ----------------
    [Header("Engine")]
    public float maxMotorTorque = 1350f;   // smoother power
    public float maxRPM = 6200f;
    public float idleRPM = 900f;
    public AnimationCurve torqueCurve;     // not heavily used

    // ---------------- CLUTCH ----------------
    [Header("Clutch")]
    [Range(0f, 1f)] public float clutch;
    public float clutchEngageSpeed = 4f;

    // ---------------- GEARBOX ----------------
    [Header("Gearbox")]
    public float reverseGearRatio = -3.0f;
    public float[] forwardGearRatios = { 2.8f, 2.0f, 1.5f, 1.2f, 1.0f };
    public float finalDriveRatio = 3.6f;

    public int currentGear = 0;

    // ---------------- RPM RULES ----------------
    [Header("RPM Rules")]
    public float stallRPM = 700f;
    public float optimalUpshiftRPM = 5400f;
    public float optimalDownshiftRPM = 1800f;

    // ---------------- STEERING ----------------
    [Header("Steering")]
    public float maxSteerAngle = 32f;
    public float steerResponse = 7f;

    // ---------------- BRAKES ----------------
    [Header("Brakes")]
    public float brakeForce = 4200f;

    // ---------------- FRICTION ----------------
    [Header("Friction")]
    public float rearSideFriction = 1.3f;
    public float rearHandbrakeFriction = 0.75f;

    // ---------------- SPEED ----------------
    [Header("Speed")]
    public float maxSpeed = 85f;

    [HideInInspector] public float engineRPM;
    [HideInInspector] public bool engineStalled;

    [Header("Gear Behavior")]
    public float minSpeedForGearFactor = 8f;   // tuning knob
    public float luggingPenalty = 0.15f;       // how dead high gear feels at low speed
    public float overRevPenalty = 0.5f;        // penalty at too high RPM

    private void Start()
    {
        rb.centerOfMass = new Vector3(0f, -0.55f, 0f); // more stable
    }
}
