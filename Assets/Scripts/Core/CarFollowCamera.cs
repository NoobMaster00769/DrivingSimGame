using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CarFollowCamera : MonoBehaviour
{
    public Transform target;

    [Header("Position")]
    public Vector3 baseOffset = new Vector3(0f, 6f, -11f);
    public float speedPullback = 3f;
    public float speedLift = 0.6f;

    [Header("Rotation")]
    public float yawLag = 0.25f;
    public float fixedPitch = 12f;
    public float bankAmount = 1.2f;
    public float bankSmooth = 0.5f;

    [Header("FOV")]
    public float baseFOV = 62f;
    public float maxFOVBoost = 7f;
    public float fovSmooth = 2f;

    [Header("Speed Reference")]
    public float maxSpeed = 40f;

    Camera cam;
    Rigidbody rb;
    Vector3 posVelocity;
    float smoothYaw;
    float yawVelocity;
    float smoothBank;
    float bankVelocity;
    float smoothYawRate;
    float yawRateVelocity;

    void Start()
    {
        cam = GetComponent<Camera>();
        rb = target ? target.GetComponent<Rigidbody>() : null;
        transform.position = target ? target.TransformPoint(baseOffset) : transform.position;
        smoothYaw = target ? target.eulerAngles.y : 0f;
        cam.fieldOfView = baseFOV;
    }

    void LateUpdate()
    {
        if (!target) return;

        float speed = rb ? rb.velocity.magnitude : 0f;
        float speed01 = Mathf.Clamp01(speed / maxSpeed);

        Vector3 localOffset = baseOffset;
        localOffset.z -= speedPullback * speed01;
        localOffset.y += speedLift * speed01;

        Vector3 desiredPos = target.TransformPoint(localOffset);
        float smoothTime = Mathf.Lerp(0.18f, 0.09f, speed01);  // more inertia at rest

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos,
            ref posVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);

        float targetYaw = target.eulerAngles.y;
        float prevYaw = smoothYaw;

        smoothYaw = Mathf.SmoothDampAngle(
            smoothYaw, targetYaw,
            ref yawVelocity, yawLag, Mathf.Infinity, Time.deltaTime);

   
        Quaternion baseRot = Quaternion.Euler(fixedPitch, smoothYaw, 0f);

        float rawYawRate = Mathf.DeltaAngle(prevYaw, smoothYaw) / Time.deltaTime;
        smoothYawRate = Mathf.SmoothDamp(
            smoothYawRate, rawYawRate,
            ref yawRateVelocity, 0.2f, Mathf.Infinity, Time.deltaTime);

   
        float targetBank = Mathf.Clamp(
            -smoothYawRate * 0.05f * (speed01 * speed01),  
            -bankAmount, bankAmount);

        smoothBank = Mathf.SmoothDampAngle(
            smoothBank, targetBank,
            ref bankVelocity, bankSmooth, Mathf.Infinity, Time.deltaTime);

        transform.rotation = baseRot * Quaternion.AngleAxis(smoothBank, Vector3.forward);

     
        float targetFOV = baseFOV + maxFOVBoost * speed01;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovSmooth);
    }
}