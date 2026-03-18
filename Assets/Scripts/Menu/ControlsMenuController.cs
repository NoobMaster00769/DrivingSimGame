using UnityEngine;
using UnityEngine.InputSystem;

public class ControlsMenuController : MonoBehaviour
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

        // 🔥 UNIVERSAL BACK
        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
        {
            TriggerBack();
            return;
        }

        float horizontal = 0f;
        bool select = false;

        if (Gamepad.current != null)
        {
            horizontal += Gamepad.current.leftStick.x.ReadValue();

            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                select = true;
        }

        horizontal += input.Steering;

        if (input.Brake > 0.5f)
            select = true;

        if (horizontal > 0.6f) ChangeIndex(1);
        if (horizontal < -0.6f) ChangeIndex(-1);

        if (select) ActivateOption();
    }

    void TriggerBack()
    {

        index = 1;
        ActivateOption();
    }

    void ChangeIndex(int direction)
    {
        halos[index].Highlight(false);

        index += direction;

        if (index >= options.Length) index = 0;
        if (index < 0) index = options.Length - 1;

        halos[index].Highlight(true);

        guidingStar.GetComponent<GuidingStarFlash>()?.TriggerFlash();
        halos[index].Pulse();

        timer = 0;
    }
    void OnEnable()
    {
        ResetSelection();
        FindObjectOfType<NavigationFooterUI>()
    .SetMenuType(NavigationFooterUI.MenuType.Horizontal);
    }
    void ResetSelection()
    {
        index = 0;

        for (int i = 0; i < halos.Length; i++)
            halos[i].Highlight(false);

        if (halos.Length > 0)
            halos[0].Highlight(true);

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
        var manager = FindObjectOfType<MenuManager>();

        if (index == 0) manager.OpenMenu(4); // Keyboard
        if (index == 2) manager.OpenMenu(5); // Controller
        if (index == 1) manager.OpenMenu(1); // Back
    }
}