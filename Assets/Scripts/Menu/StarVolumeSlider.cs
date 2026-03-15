using System.Collections.Generic;
using UnityEngine;

public class StarVolumeSlider : MonoBehaviour
{
    [Header("Setup")]
    public GameObject starPrefab;
    public int starCount = 10;
    public float spacing = 4f;

    [Header("Visual")]
    public float activeScale = 1.25f;
    public float inactiveScale = 0.6f;

    public float activeAlpha = 1f;
    public float inactiveAlpha = 0.2f;

    List<Transform> stars = new();
    List<Renderer> starRenderers = new();

    float value;

    void Start()
    {
        BuildStars();
    }

    void Update()
    {
        AnimateStars();
    }

    void BuildStars()
    {
        for (int i = 0; i < starCount; i++)
        {
            GameObject star = Instantiate(starPrefab, transform);
            star.transform.localScale = Vector3.one * 2f;

            float offset = (starCount - 1) * spacing * 0.5f;

            star.transform.localPosition =
                new Vector3(i * spacing - offset, 0, 0);
            star.transform.localRotation = Quaternion.identity;

            stars.Add(star.transform);
            starRenderers.Add(star.GetComponent<Renderer>());
        }
    }

    void AnimateStars()
    {
        int activeStars =
            Mathf.RoundToInt(value * starCount);

        for (int i = 0; i < stars.Count; i++)
        {
            bool active = i < activeStars;

            float targetScale =
                active ? activeScale : inactiveScale;

            stars[i].localScale =
                Vector3.Lerp(
                    stars[i].localScale,
                    Vector3.one * targetScale,
                    Time.deltaTime * 8f
                );

            Color c = starRenderers[i].material.color;

            float targetAlpha =
                active ? activeAlpha : inactiveAlpha;

            c.a =
                Mathf.Lerp(
                    c.a,
                    targetAlpha,
                    Time.deltaTime * 8f
                );

            starRenderers[i].material.color = c;
        }
    }

    public void SetValue(float v)
    {
        value = Mathf.Clamp01(v);
    }
}