using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    public Image fadeImage;

    void Awake()
    {
        Instance = this;

        // 🔥 FORCE BLACK ON LOAD
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
        }
    }

    public IEnumerator FadeOut(float duration)
    {
        float elapsed = 0f;

        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / duration;
            c.a = t;

            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    public IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;

        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / duration;
            c.a = 1f - t;

            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
    }
}