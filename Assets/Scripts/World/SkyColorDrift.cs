using UnityEngine;

public class SkyColorDrift : MonoBehaviour
{
    public Material skyMaterial;

    public Color colorA;
    public Color colorB;

    public float driftSpeed = 0.01f;

    private float t;

    void Update()
    {
        t += Time.deltaTime * driftSpeed;
        float blend = (Mathf.Sin(t) + 1f) * 0.5f;

        Color driftColor = Color.Lerp(colorA, colorB, blend);
        skyMaterial.SetColor("_CubemapTint", driftColor);
    }
}
