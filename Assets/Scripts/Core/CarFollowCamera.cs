using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CarFollowCamera : MonoBehaviour
{
    public Transform target;
    public PlayerDriveMetrics metrics;

    [Header("Offsets")]
    public Vector3 baseOffset = new Vector3(0f, 4f, -8f);

    [Header("Smoothing")]
    public float positionSmooth = 6f;
    public float rotationSmooth = 8f;

    [Header("Look Ahead")]
    public float lookAheadDistance = 10f;

    [Header("FOV Settings")]
    public float baseFOV = 65f;
    public float maxFOV = 75f;
    public float flowFOV = 60f;

    Camera cam;
    Rigidbody rb;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target)
            rb = target.GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (!target || !rb) return;

        Vector3 velocity = rb.velocity;
        float speed = velocity.magnitude;

        // Dynamic offset (pull back slightly at high speed)
        Vector3 dynamicOffset =
            baseOffset - new Vector3(0f, 0f, speed * 0.05f);

        Vector3 desiredPos =
            target.TransformPoint(dynamicOffset);

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            Time.deltaTime * positionSmooth
        );

        // Look ahead
        Vector3 lookPoint =
            target.position +
            velocity.normalized * lookAheadDistance;

        Quaternion targetRot =
            Quaternion.LookRotation(
                lookPoint - transform.position,
                Vector3.up
            );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSmooth
        );

        UpdateFOV(speed);
        ApplyBankRoll();
    }

    void UpdateFOV(float speed)
    {
        float intensity =
            metrics ? metrics.intensity : 0f;

        float flow =
            metrics ? metrics.flow : 0f;

        float targetFOV =
            Mathf.Lerp(baseFOV, maxFOV, intensity);

        // Calm flow narrows FOV slightly
        targetFOV =
            Mathf.Lerp(targetFOV, flowFOV, flow * 0.5f);

        cam.fieldOfView =
            Mathf.Lerp(cam.fieldOfView,
                       targetFOV,
                       Time.deltaTime * 2f);
    }

    void ApplyBankRoll()
    {
        if (!metrics) return;

        float roll =
            Mathf.Lerp(-2f, 2f, metrics.intensity);

        Vector3 euler = transform.eulerAngles;
        euler.z = Mathf.LerpAngle(euler.z, roll, Time.deltaTime * 2f);
        transform.eulerAngles = euler;
    }
}