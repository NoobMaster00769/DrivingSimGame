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

    [Header("FOV")]
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

        Vector3 dynamicOffset =
            baseOffset - new Vector3(0f, 0f, speed * 0.05f);

        Vector3 desiredPos =
            target.TransformPoint(dynamicOffset);

        float dreamFloat =
            Mathf.Sin(Time.time * 0.3f) * 0.1f;

        desiredPos += Vector3.up * dreamFloat;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            Time.deltaTime * positionSmooth
        );

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

        UpdateFOV();
    }

    void UpdateFOV()
    {
        if (!metrics) return;

        float targetFOV =
            Mathf.Lerp(baseFOV, maxFOV, metrics.intensity * 0.7f);

        targetFOV =
            Mathf.Lerp(targetFOV, flowFOV, metrics.flow * 0.5f);

        cam.fieldOfView =
            Mathf.Lerp(cam.fieldOfView,
                       targetFOV,
                       Time.deltaTime * 2f);
    }
}