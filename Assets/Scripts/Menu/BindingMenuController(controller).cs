using UnityEngine;
using UnityEngine.InputSystem;

public class BindingListMenuController_controller : MonoBehaviour
{
    public Transform[] options;
    public Transform guidingStar;
    public VehicleInputReader input;

    public float starOffsetX = -8f;

    int index;
    float timer;
    float cooldown = 0.25f;

    bool rebinding;

    Vector3 velocity;

    enum InputMode { Keyboard, Controller }
    InputMode currentMode = InputMode.Controller;

    void Update()
    {
        if (!enabled) return;

        var manager = FindObjectOfType<MenuManager>();
        if (manager && manager.IsTransitioning()) return;

        DetectInputMode();

        timer += Time.unscaledDeltaTime;

        if (!rebinding)
            HandleNavigation();

        MoveGuidingStar();
        AnimateSelection();
    }

    // --------------------------------------------------
    // 🔥 AUTO INPUT DETECTION
    void DetectInputMode()
    {
        // any key pressed → keyboard mode
        if (Keyboard.current.anyKey.wasPressedThisFrame)
        {
            currentMode = InputMode.Keyboard;
        }

        // any gamepad input → controller mode
        if (Gamepad.current != null)
        {
            if (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.01f ||
                Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                currentMode = InputMode.Controller;
            }
        }
    }

    void OnEnable()
    {
        index = 0;
        timer = 0;
        FindObjectOfType<NavigationFooterUI>()
    .SetMenuType(NavigationFooterUI.MenuType.Vertical);
    }

    // --------------------------------------------------

    void HandleNavigation()
    {        // 🔥 ESC handling FIRST (state-based)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (rebinding)
            {
                // cancel rebind ONLY
                rebinding = false;
                return;
            }
            else
            {
                // go back menu
                FindObjectOfType<MenuManager>().OpenMenu(3);
                return;
            }
        }
        if (timer < cooldown) return;

        float vertical = 0f;
        bool select = false;
        bool cancel = false;

        // 🎮 CONTROLLER INPUT
        if (Gamepad.current != null)
        {
            vertical += Gamepad.current.leftStick.y.ReadValue();

            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                select = true;

            if (Gamepad.current.buttonEast.wasPressedThisFrame)
                cancel = true;
        }

        // ⌨️ KEYBOARD INPUT (ALSO ALLOWED HERE)
        // Keyboard
        if (Keyboard.current.wKey.isPressed) vertical += 1f;
        if (Keyboard.current.sKey.isPressed) vertical -= 1f;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            select = true;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            cancel = true;

        // --------------------

        if (vertical > 0.5f)
            ChangeIndex(-1);

        if (vertical < -0.5f)
            ChangeIndex(1);

        if (select)
            Activate();

        if (cancel && rebinding)
            rebinding = false;

    }

    // --------------------------------------------------

    void ChangeIndex(int dir)
    {
        index += dir;

        if (index >= options.Length)
            index = 0;

        if (index < 0)
            index = options.Length - 1;

        timer = 0;
    }

    // --------------------------------------------------

    void Activate()
    {
        var item = options[index].GetComponent<ControllerRebindItem>();

        if (item.isBack)
        {
            FindObjectOfType<MenuManager>().OpenMenu(3);
            return;
        }

        rebinding = true;

        item.StartRebind(() =>
        {
            rebinding = false;

            var actions = input.GetActions();
            PlayerPrefs.SetString("rebinds",
                actions.asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        });

        timer = 0;
    }

    // --------------------------------------------------

    void MoveGuidingStar()
    {
        Vector3 target =
            options[index].position +
            Vector3.left * starOffsetX;
        guidingStar.position = Vector3.SmoothDamp(
            guidingStar.position,
            target,
            ref velocity,
            0.15f,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }

    // --------------------------------------------------

    void AnimateSelection()
    {
        for (int i = 0; i < options.Length; i++)
        {
            var item = options[i].GetComponent<ControllerRebindItem>();
            item.SetSelected(i == index);
        }
    }
}