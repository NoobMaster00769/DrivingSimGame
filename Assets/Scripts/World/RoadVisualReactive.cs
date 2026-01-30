using UnityEngine;

public class RoadVisualReactive : MonoBehaviour
{
    public RoadState roadState;
    public Renderer roadRenderer;

    [Header("Colors")]
    public Color calmColor = new Color(0.15f, 0.15f, 0.15f);
    public Color aggressiveColor = new Color(0.25f, 0.25f, 0.25f);

    [Header("Smoothness")]
    public float calmSmoothness = 0.15f;
    public float aggressiveSmoothness = 0.35f;

    public float smoothSpeed = 2f;

    Material mat;

    void Start()
    {
        if (!roadRenderer)
            roadRenderer = GetComponent<Renderer>();

        mat = roadRenderer.material; // instance material
    }

    void Update()
    {
        if (!roadState || !mat) return;

        float t = roadState.agitation;

        Color targetColor = Color.Lerp(calmColor, aggressiveColor, t);
        mat.color = Color.Lerp(mat.color, targetColor, Time.deltaTime * smoothSpeed);

        if (mat.HasProperty("_Glossiness"))
        {
            float targetSmoothness =
                Mathf.Lerp(calmSmoothness, aggressiveSmoothness, t);

            float current = mat.GetFloat("_Glossiness");
            mat.SetFloat(
                "_Glossiness",
                Mathf.Lerp(current, targetSmoothness, Time.deltaTime * smoothSpeed)
            );
        }
    }
}
