using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CosmicExposure : MonoBehaviour
{
    public Volume volume;
    ColorAdjustments color;

    void Start()
    {
        volume.profile.TryGet(out color);
    }

    void Update()
    {
        float pulse = Mathf.Sin(Time.time * 0.2f) * 0.05f;
        color.postExposure.value = 0.2f + pulse;
    }
}