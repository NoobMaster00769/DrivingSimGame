using UnityEngine;

public class GuidingStarPulse : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.2f;

    Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float pulse =
            1 + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        transform.localScale = baseScale * pulse;
    }
}