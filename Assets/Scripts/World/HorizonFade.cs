using UnityEngine;

public class HorizonFade : MonoBehaviour
{
    public Transform cameraTransform;
    public float fadeStart = 100f;
    public float fadeEnd = 300f;

    private Material mat;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        float d = Vector3.Distance(cameraTransform.position, transform.position);
        float t = Mathf.InverseLerp(fadeStart, fadeEnd, d);
        Color c = mat.color;
        c.a = Mathf.Lerp(0.25f, 0f, t);
        mat.color = c;
    }
}
