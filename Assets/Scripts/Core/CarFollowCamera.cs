using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CarFollowCamera : MonoBehaviour
{
    public Transform target;

    [Header("FOV")]
    public float baseFOV = 65f;   // slightly wider base — more road visible
    public float fovSmooth = 2f;

    [Header("Void Effect")]
    public float voidFOV = 30f;
    public float voidFOVSpeed = 3f;
    public float voidShake = 0.4f;
    float voidBlend = 0f;

    [Header("Framing")]
    public Vector3 baseOffset = new Vector3(0f, 5.5f, -10f);

    [Header("Follow")]
    // Lower = camera has more inertia / cinematic lag
    // Higher = glues to car
    // 8–10 gives a loose, weighty follow without losing the car
    public float positionSmooth = 9f;
    public float rotationSmooth = 5f;

    [Header("Speed Offset")]
    public float speedPushBack = 3f;    // how far camera pulls back at max speed
    public float speedLift = 0.8f;

    [Header("Look Ahead")]
    public float lookAheadDistance = 14f;
    public float lookAheadSmooth = 2.5f;  // higher = smoother anticipation

    [Header("Turn Banking")]
    public float bankAmount = 5f;           // now actually used (was hardcoded to 5)

    [Header("Motion")]
    public float breathAmount = 0.35f;
    public float breathSpeed = 0.1f;
    public float driftAmount = 0.25f;
    public float driftSpeed = 0.12f;
    public float suspensionAmount = 0.18f;
    public float suspensionSpeed = 6f;

    [Header("Turn Anticipation")]
    public float anticipationAmount = 4f;

    // ── Internal ─────────────────────────────────────────────────
    Camera cam;
    Rigidbody rb;

    Vector3 smoothVelocity;          // SmoothDamp velocity for position
    Vector3 smoothLookDir;
    float breathTimer;
    float driftTimer;
    float suspensionTimer;

    // Cinematic lean — camera tilts INTO curves slightly (not banking, tilting)
    float leanAngle;
    float leanVelocity;

    // Soft velocity tracking — lags behind actual velocity for cinematic weight
    Vector3 softVelocityDir;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target) rb = target.GetComponent<Rigidbody>();
        smoothLookDir = target ? target.forward : Vector3.forward;
        softVelocityDir = smoothLookDir;
        cam.fieldOfView = baseFOV;
    }

    void LateUpdate()
    {
        if (!target) return;

        float speed = rb ? rb.velocity.magnitude : 0f;
        // Use actual meaningful max (28 m/s from inspector) not the code default
        float speed01 = Mathf.Clamp01(speed / 28f);

        // ── Velocity direction (soft — lags 0.3 s behind real velocity) ──
        Vector3 realVelDir = rb && rb.velocity.sqrMagnitude > 0.5f
            ? rb.velocity.normalized
            : target.forward;

        softVelocityDir = Vector3.Lerp(
            softVelocityDir,
            realVelDir,
            Time.deltaTime * 1.8f);

        // ── POSITION ─────────────────────────────────────────────
        // Speed pushback and lift scale from 0 at rest to full at max speed.
        // Because we use SmoothDamp the camera has genuine inertia — it takes
        // ~0.2 s to catch a sudden move, giving a cinematic "weight" that
        // positionSmooth alone can't achieve.
        Vector3 offset = baseOffset;
        offset.z -= speedPushBack * speed01;          // pull back at speed
        offset.y += speedLift * speed01 * 0.5f;   // lift gently

        Vector3 desired = target.TransformPoint(offset);

        float smoothTime = Mathf.Lerp(0.12f, 0.06f, speed01); // looser at rest
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref smoothVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.deltaTime);

        // ── FOV ──────────────────────────────────────────────────
        // Void effect overrides everything
        bool inVoid = LevelLayoutGenerator.isInVoid;
        voidBlend = Mathf.Lerp(voidBlend, inVoid ? 1f : 0f, Time.deltaTime * voidFOVSpeed);

        // FOV breathes gently with speed — 10 degrees total swing.
        // This is ADDITIVE not multiplicative so it never feels like the world
        // is zooming in/out — just a natural widening as you pick up speed.
        float speedFOV = baseFOV + speed01 * 10f;
        float targetFOV = Mathf.Lerp(speedFOV, voidFOV, voidBlend);

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            Time.deltaTime * fovSmooth);

        // ── LOOK AHEAD ───────────────────────────────────────────
        // Smooth look direction follows soft velocity (not snappy real velocity).
        // This means on a curve the camera pans ahead of the car slowly — feels
        // like a real camera operator rather than a locked gimbal.
        smoothLookDir = Vector3.Lerp(
            smoothLookDir,
            softVelocityDir,
            Time.deltaTime * (lookAheadSmooth * 0.6f));

        // Look-ahead point: further at speed, closer at rest (more intimate)
        float aheadDist = Mathf.Lerp(lookAheadDistance * 0.6f, lookAheadDistance, speed01);
        Vector3 lookPoint = target.position + smoothLookDir * aheadDist;

        Quaternion targetRot = Quaternion.LookRotation(
            lookPoint - transform.position,
            Vector3.up);

        // ── BANKING (uses Inspector value now) ───────────────────
        // Bank INTO the turn direction — feels like a camera on a boom arm.
        float sideways = Vector3.Dot(target.right, softVelocityDir);
        float targetLean = -sideways * bankAmount * speed01;  // zero at rest
        leanAngle = Mathf.SmoothDamp(leanAngle, targetLean, ref leanVelocity, 0.2f);

        Quaternion bankRot = Quaternion.AngleAxis(leanAngle, Vector3.forward);

        float rotSmooth = Mathf.Lerp(3f, rotationSmooth, speed01);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot * bankRot,
            Time.deltaTime * rotSmooth);

        // ── VOID SHAKE ───────────────────────────────────────────
        if (voidBlend > 0.05f)
        {
            float shake = Mathf.PerlinNoise(Time.time * 8f, 0f) - 0.5f;
            transform.position += transform.right * shake * voidShake * voidBlend;
        }
    }
}