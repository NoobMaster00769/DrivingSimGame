using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChromaticDrift : MonoBehaviour
{
    public DrivingStress stressSource;
    public Volume volume;

    ChromaticAberration chroma;

    public float maxIntensity = 0.05f;

    void Start()
    {
        if (volume == null) return;
        volume.profile.TryGet(out chroma);
    }

    void Update()
    {
        if (chroma == null || stressSource == null) return;

        float target = stressSource.stress * maxIntensity;
        chroma.intensity.value = Mathf.Lerp(
            chroma.intensity.value,
            target,
            Time.deltaTime * 2f
        );
    }
}
