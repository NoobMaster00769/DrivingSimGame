using UnityEngine;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        // 🔥 wait until ScreenFader is ready
        while (ScreenFader.Instance == null)
            yield return null;

        yield return new WaitForEndOfFrame();

        yield return ScreenFader.Instance.FadeIn(1f);
    }
}