using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CarFollowCamera : MonoBehaviour
{
    public Transform target;

    [Header("Speed FOV")]
    public float baseFOV = 55f;
    public float speedFOV = 6f;
    public float fovSmooth = 2f;
   
    [Header("Void Effect")]
    public float voidFOV = 30f;
    public float voidFOVSpeed = 3f;
    public float voidShake = 0.4f;
    float voidBlend = 0f;

    [Header("Framing")]
    public Vector3 baseOffset = new Vector3(0f, 7f, -11f);

    [Header("Follow")]
    public float positionSmooth = 3.5f;
    public float rotationSmooth = 3.2f;

    [Header("Look Ahead")]
    public float lookAheadDistance = 26f;
    public float lookAheadSmooth = 3f;

    [Header("Turn Banking")]
    public float bankAmount = 8f;
    public float bankSmooth = 3f;

    [Header("Breathing Motion")]
    public float breathAmount = 0.35f;
    public float breathSpeed = 0.35f;

    [Header("Cosmic Drift")]
    public float driftAmount = 0.25f;
    public float driftSpeed = 0.12f;

    [Header("Suspension Motion")]
    public float suspensionAmount = 0.18f;
    public float suspensionSpeed = 6f;

    [Header("Turn Anticipation")]
    public float anticipationAmount = 3f;

    [Header("Forward Bias")]
    public float forwardBiasStrength = 2.5f;

    float suspensionTimer;

    Camera cam;
    Rigidbody rb;

    Vector3 velocity;
    Vector3 smoothLookDir;

    float breathTimer;
    float driftTimer;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (target)
            rb = target.GetComponent<Rigidbody>();

        smoothLookDir = target.forward;

        cam.fieldOfView = 55f;
    }

    void LateUpdate()
    {
        if (!target) return;

        breathTimer += Time.deltaTime * breathSpeed;
        driftTimer += Time.deltaTime * driftSpeed;

        //---------------------------------
        // FOLLOW POSITION
        //---------------------------------

        Vector3 desiredPosition = target.TransformPoint(baseOffset);

        // breathing motion (camera subtly moves forward/back)
        float breath = Mathf.Sin(breathTimer) * breathAmount;
        desiredPosition += target.forward * breath;

        float speed = rb ? rb.velocity.magnitude : 0f;

        float targetFOV =
            baseFOV + Mathf.Clamp(speed * 0.15f, 0f, speedFOV);

        float targetVoid = LevelLayoutGenerator.isInVoid ? 1f : 0f;

        voidBlend = Mathf.Lerp(voidBlend, targetVoid, Time.deltaTime * 2f);

        float finalFOV = Mathf.Lerp(targetFOV, voidFOV, voidBlend);



        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            finalFOV,
            Time.deltaTime * voidFOVSpeed
        );

        // 🔥 VOID GRAVITY PULL
        if (voidBlend > 0.01f)
        {
            desiredPosition += Vector3.down * voidBlend * 2.5f;
        }// 🔥 DARKEN VIEW DIRECTION
        Vector3 forwardDrop =
            transform.forward * -voidBlend * 2f;

        desiredPosition += forwardDrop;
        suspensionTimer += Time.deltaTime * suspensionSpeed;

        float suspension =
            Mathf.Sin(suspensionTimer) * suspensionAmount;

        desiredPosition += target.up * suspension;

        float baseShake =
            Mathf.PerlinNoise(Time.time * 6f, 0f) *
            Mathf.Clamp01(speed * 0.02f) * 0.2f;

        float voidExtra =
            voidBlend * voidShake * 1.5f;

        float shake = baseShake + voidExtra;

        desiredPosition += target.up * shake;

        // cosmic floating drift
        float drift = Mathf.Sin(driftTimer) * driftAmount;
        desiredPosition += target.right * drift;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            1f / positionSmooth
        );

        //---------------------------------
        // LOOK AHEAD
        //---------------------------------



        Vector3 forwardDir =
            rb && rb.velocity.sqrMagnitude > 0.5f
            ? rb.velocity.normalized
            : target.forward;

        smoothLookDir = Vector3.Lerp(
            smoothLookDir,
            forwardDir,
            Time.deltaTime * lookAheadSmooth
        );

        Vector3 turnOffset =
    target.right * Vector3.Dot(target.right, smoothLookDir) * anticipationAmount;

        Vector3 lookPoint =
            target.position +
            smoothLookDir * lookAheadDistance +
            turnOffset;

        Quaternion baseRotation =
            Quaternion.LookRotation(
                lookPoint - transform.position,
                Vector3.up
            );

        //---------------------------------
        // CAMERA BANKING (emotion)
        //---------------------------------

        float sideways =
            Vector3.Dot(target.right, smoothLookDir);

        float bank =
            Mathf.Clamp(-sideways * bankAmount, -bankAmount, bankAmount);

        float turnImpact =
    Mathf.Abs(sideways) * 1.5f;

        desiredPosition += target.right * turnImpact;

        Quaternion bankRot =
            Quaternion.AngleAxis(bank, Vector3.forward);

        //---------------------------------
        // APPLY ROTATION
        //---------------------------------

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            baseRotation * bankRot,
            Time.deltaTime * (rotationSmooth * 0.9f)
        );
    }
}