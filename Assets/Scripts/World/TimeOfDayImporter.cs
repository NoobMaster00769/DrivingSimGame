using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class TimeOfDayImporter
{
    [MenuItem("World/Import Time Of Day From Scene")]
    static void Import()
    {
        var sun = Object.FindObjectOfType<Light>();
        if (sun == null)
        {
            Debug.LogError("No Directional Light found in scene.");
            return;
        }

        var asset = ScriptableObject.CreateInstance<TimeOfDay>();

        // SKY
        asset.skybox = RenderSettings.skybox;
        asset.skyExposure = RenderSettings.skybox.HasProperty("_Exposure")
            ? RenderSettings.skybox.GetFloat("_Exposure")
            : 1f;

        // FOG
        asset.fogEnabled = RenderSettings.fog;
        asset.fogColor = RenderSettings.fogColor;
        asset.fogDensity = RenderSettings.fogDensity;

        // AMBIENT
        asset.ambientColor = RenderSettings.ambientLight;

        // SUN
        asset.sunColor = sun.color;
        asset.sunIntensity = sun.intensity;

        // SAVE
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string path = $"Assets/World/{sceneName}_TimeOfDay.asset";

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"TimeOfDay imported from scene: {sceneName}");
    }
}
