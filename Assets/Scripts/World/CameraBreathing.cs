using UnityEngine;

public class CameraBreathing : MonoBehaviour
{
    public float positionAmplitude = 0.05f;
    public float rotationAmplitude = 0.3f;
    public float speed = 0.4f;

    private Vector3 startPos;
    private Quaternion startRot;

    void Start()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;
    }

    void Update()
    {
        float t = Time.time * speed;

        float yOffset = Mathf.Sin(t) * positionAmplitude;
        float rotOffset = Mathf.Sin(t * 0.8f) * rotationAmplitude;

        transform.localPosition = startPos + new Vector3(0, yOffset, 0);
        transform.localRotation = startRot * Quaternion.Euler(rotOffset, 0, 0);
    }
}
