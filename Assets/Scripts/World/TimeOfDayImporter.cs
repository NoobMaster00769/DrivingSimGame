using UnityEngine;
using UnityEditor;

public class TimeOfDayImporter
{
    [MenuItem("World/Import Time Of Day From Scene")]
    static void Import()
    {
        Light sun = Object.FindObjectOfType<Light>();
        if (sun == null || sun.type != LightType.Directional)
        {
            Debug.LogError("No Directional Light found in scene.");
            return;
        }

        TimeOfDay asset = ScriptableObject.CreateInstance<TimeOfDay>();

        // -------- SKYBOX --------
        asset.skybox = RenderSettings.skybox;

        if (RenderSettings.skybox != null &&
            RenderSettings.skybox.HasProperty("_Exposure"))
        {
            asset.skyExposure = RenderSettings.skybox.GetFloat("_Exposure");
        }
        else
        {
            asset.skyExposure = 1f;
        }

        // -------- SUN --------
        asset.sunColor = sun.color;
        asset.sunIntensity = sun.intensity;

        // -------- HARD RESET (NO TINT DATA) --------
        asset.fogEnabled = false;
        asset.fogColor = Color.clear;
        asset.fogDensity = 0f;

        asset.ambientColor = Color.white; // UNUSED but neutral

        string sceneName = UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().name;

        string path = $"Assets/World/{sceneName}_TimeOfDay.asset";

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TimeOfDayImporter] Imported SAFE preset from {sceneName}");
    }
}
