using UnityEngine;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    public TMP_Text text;
    public CanvasGroup canvasGroup;

    public float fadeDuration = 0.25f;

    void Awake()
    {
        // Ensure CanvasGroup exists
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        Clear(); // start hidden
    }

    public void Show(string message)
    {
        if (text == null)
        {
            Debug.LogError("TutorialUI TEXT NOT ASSIGNED");
            return;
        }

        text.text = message;
        text.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    public void Clear()
    {
        if (text != null)
            text.gameObject.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    System.Collections.IEnumerator FadeIn()
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public void HideSmooth()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        if (canvasGroup == null)
        {
            Clear();
            yield break;
        }

        float t = 0f;
        float start = canvasGroup.alpha;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        if (text != null)
            text.gameObject.SetActive(false);
    }
}