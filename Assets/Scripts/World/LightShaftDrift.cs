using UnityEngine;

public class LightShaftDrift : MonoBehaviour
{
    public float swaySpeed = 0.08f;
    public float swayAmount = 4f;
    public float pulseSpeed = 0.5f;

    Vector3 basePos;
    Material mat;
    float randomOffset;

    void Start()
    {
        basePos = transform.position;
        mat = GetComponentInChildren<MeshRenderer>().material;
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float sway = Mathf.Sin((Time.time + randomOffset) * swaySpeed) * swayAmount;
        transform.position = basePos + new Vector3(sway, 0f, 0f);

        float alphaPulse = 0.06f + Mathf.Sin((Time.time + randomOffset) * pulseSpeed) * 0.02f;

        Color c = mat.color;
        c.a = alphaPulse;
        mat.color = c;
    }
}