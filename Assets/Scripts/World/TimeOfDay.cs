using UnityEngine;

[CreateAssetMenu(menuName = "World/Time Of Day Preset")]
public class TimeOfDay : ScriptableObject
{
    [Header("Sky")]
    public Material skybox;
    public float skyExposure = 1f;

    [Header("Sun")]
    public Color sunColor = Color.white;
    public float sunIntensity = 1f;

    [Header("Fog")]
    public bool fogEnabled = true;
    public Color fogColor = Color.gray;
    public float fogDensity = 0.01f;

    [Header("Ambient")]
    public Color ambientColor = Color.gray;
}
