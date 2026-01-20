using UnityEngine;

public class VehicleContext : MonoBehaviour
{
    [Header("Differential (LSD)")]
    [Range(0f, 1f)]
    public float lsdStrength = 0.6f;
    // 0 = open diff, 1 = fully locked

    public float maxLsdBias = 3.0f;
    // Maximum torque bias ratio
  
    [Header("Traction Control")]
    public bool tractionControlEnabled = true;

    [Tooltip("Allowed slip before TC kicks in")]
    public float slipThreshold = 0.15f;

    [Tooltip("How aggressively TC cuts torque")]
    public float tcStrength = 6f;

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
    public float maxMotorTorque = 1600f;
    public float maxRPM = 6500f;
    public float idleRPM = 900f;
    public AnimationCurve torqueCurve;

    // ---------------- CLUTCH ----------------
    [Header("Clutch")]
    [Range(0f, 1f)] public float clutch; // 0 = engaged, 1 = disengaged
    public float clutchEngageSpeed = 4f;

    // ---------------- GEARBOX ----------------
    [Header("Gearbox")]
    public float reverseGearRatio = -3.0f;
    public float[] forwardGearRatios = { 3.2f, 2.1f, 1.5f, 1.1f, 0.9f };
    public float finalDriveRatio = 3.4f;

    [Tooltip("-1 = Reverse, 0 = Neutral, 1+ = Forward gears")]
    public int currentGear = 0;

    // ---------------- RPM RULES ----------------
    [Header("RPM Rules")]
    public float stallRPM = 700f;
    public float optimalUpshiftRPM = 5200f;
    public float optimalDownshiftRPM = 1800f;

    // ---------------- STEERING ----------------
    [Header("Steering")]
    public float maxSteerAngle = 30f;
    public float steerResponse = 6f;

    // ---------------- BRAKES ----------------
    [Header("Brakes")]
    public float brakeForce = 3000f;

    // ---------------- FRICTION ----------------
    [Header("Friction")]
    public float rearSideFriction = 1.4f;
    public float rearHandbrakeFriction = 0.6f;

    // ---------------- SPEED ----------------
    [Header("Speed")]
    public float maxSpeed = 45f;

    // ---------------- RUNTIME ----------------
    [HideInInspector] public float engineRPM;
    [HideInInspector] public bool engineStalled;

    private void Start()
    {
        rb.centerOfMass = new Vector3(0f, -0.45f, 0f);
    }
}
