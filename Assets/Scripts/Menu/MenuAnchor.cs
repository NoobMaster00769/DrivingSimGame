using UnityEngine;

public class MenuAnchor : MonoBehaviour
{
    public Transform cam;

    public float distance = 900f;
    public float height = 350f;

    public float followSpeed = 2f;

    void LateUpdate()
    {
        Vector3 target =
            cam.position +
            cam.forward * distance +
            Vector3.up * height;

        transform.position =
            Vector3.Lerp(transform.position, target, Time.deltaTime * followSpeed);

        transform.rotation =
            Quaternion.LookRotation(transform.position - cam.position);
    }
}