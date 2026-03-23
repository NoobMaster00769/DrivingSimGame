using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
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

    PauseSystem pauseSystem;
    MenuManager menuManager;

    void Start()
    {
        index = Mathf.Clamp(index, 0, options.Length - 1);

        if (halos.Length > index)
            halos[index].Highlight(true);

        // ✅ cache once (no spam FindObjectOfType)
        pauseSystem = FindObjectOfType<PauseSystem>();
        menuManager = FindObjectOfType<MenuManager>();
    }

    void Update()
    {
        if (!enabled) return;

        // 🔥 STRICT STATE CONTROL
        if (GameStateController.Instance.currentState != GameState.Paused)
            return;

        // 🔥 BLOCK DURING MENU TRANSITION
        if (menuManager != null && menuManager.IsTransitioning())
            return;

        timer += Time.unscaledDeltaTime;

        HandleNavigation();
        MoveGuidingStar();
        AnimateSelection();
    }

    void HandleNavigation()
    {
        if (timer < inputCooldown) return;

        float horizontal = 0f;
        bool select = false;

        // 🎮 Controller
        if (Gamepad.current != null)
        {
            horizontal += Gamepad.current.leftStick.x.ReadValue();

            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                select = true;
        }

        // ⌨️ Keyboard
        if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
        if (Keyboard.current.dKey.isPressed) horizontal += 1f;

        if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
            select = true;

        // Steering fallback
        horizontal += input.Steering;

        if (horizontal > 0.6f) ChangeIndex(1);
        if (horizontal < -0.6f) ChangeIndex(-1);

        if (select) ActivateOption();
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

    void ActivateOption()
    {
        if (index == 0) pauseSystem?.Resume();                 // Resume
        if (index == 1)  pauseSystem?.ExitToMainMenu();            // Settings
        if (index == 2) menuManager?.OpenMenu(1);    // Exit
    }

    void MoveGuidingStar()
    {
        Transform target = options[index];

        Vector3 targetPos = target.localPosition + Vector3.up * starHeight;

        guidingStar.localPosition = Vector3.SmoothDamp(
      guidingStar.localPosition,
      targetPos,
      ref velocity,
      0.18f,
      Mathf.Infinity,
      Time.unscaledDeltaTime
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
                Time.unscaledDeltaTime * 7f
            );
        }
    }

    void OnEnable()
    {
        ResetSelection();

        FindObjectOfType<NavigationFooterUI>()
            .SetMenuType(NavigationFooterUI.MenuType.Pause);
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
}