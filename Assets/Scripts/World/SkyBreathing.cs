using UnityEngine;

public class SkyBreathingFog : MonoBehaviour
{
    [Header("Target Material")]
    public Material skyMaterial;

    [Header("Breathing")]
    public float breathSpeed = 0.05f;     // very slow
    public float heightAmplitude = 0.05f;
    public float intensityAmplitude = 0.03f;

    [Header("Base Values")]
    public float baseFogHeight = 1.0f;
    public float baseFogIntensity = 0.15f;

    private float timeOffset;

    void Start()
    {
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!skyMaterial) return;

        float t = (Time.time + timeOffset) * breathSpeed;
        float wave = Mathf.Sin(t);

        skyMaterial.SetFloat("_FogHeight", baseFogHeight + wave * heightAmplitude);
        skyMaterial.SetFloat("_FogIntensity", baseFogIntensity + wave * intensityAmplitude);
    }
}
