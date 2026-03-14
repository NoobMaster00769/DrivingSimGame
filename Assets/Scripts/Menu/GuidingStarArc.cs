using UnityEngine;

public class GuidingStarArc : MonoBehaviour
{
    public float arcHeight = 1.2f;
    public float arcSpeed = 8f;

    Vector3 basePos;

    void Update()
    {
        basePos = transform.localPosition;

        float arc =
            Mathf.Sin(Time.time * arcSpeed) * arcHeight;

        transform.localPosition =
            basePos + Vector3.up * arc;
    }
}