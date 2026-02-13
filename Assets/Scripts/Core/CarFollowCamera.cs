using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CarFollowCamera : MonoBehaviour
{
    public Transform target;

    [Header("Framing")]
    public Vector3 baseOffset = new Vector3(0f, 6.8f, -10f);
    public float verticalBias = -3.5f;

    [Header("Weight")]
    public float positionSmooth = 1.6f;
    public float rotationSmooth = 1.8f;

    [Header("Look Ahead")]
    public float lookAheadDistance = 22f;
    public float lookAheadSmooth = 1.1f;

    [Header("Cosmic Drift")]
    public float horizonTiltAmount = 0.4f;
    public float horizonTiltSpeed = 0.015f;

    [Header("Subtle Presence Breathing")]
    public float presenceBreathAmount = 0.25f;
    public float presenceBreathSpeed = 0.035f;

    Camera cam;
    Rigidbody rb;

    float horizonTimer;
    float breathTimer;

    Vector3 smoothLookDir;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (target)
            rb = target.GetComponent<Rigidbody>();

        if (target)
            smoothLookDir = target.forward;

        cam.fieldOfView = 56f; // cinematic compression
    }

    void LateUpdate()
    {
        if (!target) return;

        horizonTimer += Time.deltaTime * horizonTiltSpeed;
        breathTimer += Time.deltaTime * presenceBreathSpeed;

        // -------------------------------
        // POSITION (Closer but Elevated)
        // -------------------------------

        Vector3 desiredPos =
            target.TransformPoint(baseOffset);

        float breath =
            Mathf.Sin(breathTimer) * presenceBreathAmount;

        desiredPos += target.forward * breath;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            Time.deltaTime * positionSmooth
        );

        // -------------------------------
        // LOOK AHEAD
        // -------------------------------

        Vector3 forwardDir =
            rb && rb.velocity.sqrMagnitude > 0.1f
            ? rb.velocity.normalized
            : target.forward;

        smoothLookDir = Vector3.Lerp(
            smoothLookDir,
            forwardDir,
            Time.deltaTime * lookAheadSmooth
        );

        Vector3 lookPoint =
            target.position +
            smoothLookDir * lookAheadDistance;

        Quaternion baseRotation =
            Quaternion.LookRotation(
                lookPoint - transform.position,
                Vector3.up
            );

        // -------------------------------
        // Subtle Cosmic Horizon Drift
        // -------------------------------

        float tilt =
            Mathf.Sin(horizonTimer) * horizonTiltAmount;

        Quaternion roll =
            Quaternion.AngleAxis(tilt, Vector3.forward);

        Quaternion pitchBias =
            Quaternion.AngleAxis(verticalBias, Vector3.right);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            baseRotation * roll * pitchBias,
            Time.deltaTime * rotationSmooth
        );
    }
}
