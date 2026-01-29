using UnityEngine;

public class CarFollowCamera : MonoBehaviour
{
    public Transform target;

    [Header("Offsets")]
    public Vector3 followOffset = new Vector3(0f, 4f, -8f);

    [Header("Smoothing")]
    public float positionSmooth = 6f;
    public float rotationSmooth = 8f;

    [Header("Look")]
    public float lookAheadDistance = 8f;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 velocity =
            target.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;

        Vector3 lookPoint =
            target.position +
            velocity.normalized * lookAheadDistance;

        Vector3 desiredPos =
            target.TransformPoint(followOffset);

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            Time.deltaTime * positionSmooth
        );

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
    }
}
