using UnityEngine;

public class GuidingStarFlash : MonoBehaviour
{
    public float flashScale = 1.6f;
    public float flashSpeed = 12f;

    Vector3 baseScale;
    float flash;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (flash > 0)
        {
            flash -= Time.deltaTime * flashSpeed;

            float scale =
                Mathf.Lerp(1f, flashScale, flash);

            transform.localScale = baseScale * scale;
        }
        else
        {
            transform.localScale =
                Vector3.Lerp(
                    transform.localScale,
                    baseScale,
                    Time.deltaTime * 6f
                );
        }
    }

    public void TriggerFlash()
    {
        flash = 1;
    }
}