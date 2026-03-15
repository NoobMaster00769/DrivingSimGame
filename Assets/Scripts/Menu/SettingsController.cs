using UnityEngine;

public class SettingsMenuController : MonoBehaviour
{
    public Transform[] options;
    public StarHaloGenerator[] halos;
    public Transform guidingStar;
    public VehicleInputReader input;

    public float starHeight = 3f;

    int index = 0;
    float inputCooldown = 0.25f;
    float timer;

    Vector3 velocity;

    void Start()
    {
        index = Mathf.Clamp(index, 0, options.Length - 1);

        if (halos.Length > index)
            halos[index].Highlight(true);
    }

    void Update()
    {
        if (!enabled) return;

        var manager = FindObjectOfType<MenuManager>();
        if (manager != null && manager.IsTransitioning())
            return;

        timer += Time.deltaTime;

        HandleNavigation();
        MoveGuidingStar();
        AnimateSelection();
    }

    void HandleNavigation()
    {
        if (timer < inputCooldown) return;

        if (input.Steering > 0.6f) ChangeIndex(1);
        if (input.Steering < -0.6f) ChangeIndex(-1);

        if (input.Brake > 0.5f) ActivateOption();
    }

    void ChangeIndex(int direction)
    {
        if (halos.Length > index)
            halos[index].Highlight(false);

        index += direction;

        if (index >= options.Length) index = 0;
        if (index < 0) index = options.Length - 1;

        if (halos.Length > index)
            halos[index].Highlight(true);

        var flash = guidingStar.GetComponent<GuidingStarFlash>();
        if (flash) flash.TriggerFlash();
        halos[index].Pulse();
        timer = 0;
    }

    void MoveGuidingStar()
    {
        Transform target = options[index];

        Vector3 targetPos = target.localPosition + Vector3.up * starHeight;

        guidingStar.localPosition = Vector3.SmoothDamp(
            guidingStar.localPosition,
            targetPos,
            ref velocity,
            0.18f
        );
    }

    void AnimateSelection()
    {
        for (int i = 0; i < options.Length; i++)
        {
            float scale = (i == index) ? 1.25f : 1f;

            options[i].localScale = Vector3.Lerp(
                options[i].localScale,
                Vector3.one * scale,
                Time.deltaTime * 7f
            );
        }
    }

    void ActivateOption()
    {
        if (index == 0)
            Debug.Log("Control Settings");

        if (index == 1)
            FindObjectOfType<MenuManager>().OpenMenu(0);

        if (index == 2)
            FindObjectOfType<MenuManager>().OpenMenu(2);
    }
}