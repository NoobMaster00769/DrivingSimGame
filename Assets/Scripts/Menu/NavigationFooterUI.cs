using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class NavigationFooterUI : MonoBehaviour
{
    public TMP_Text footerText;

    enum InputMode { Keyboard, Controller }
    InputMode currentMode;

    enum ControllerType { Xbox, PlayStation, Generic }
    ControllerType controllerType;

    public Camera menuCamera;
    public Vector3 screenOffset = new Vector3(-0.9f, 0.85f, 6f);

    [Header("Keyboard Layouts")]
    public string horizontalKeyboard = "A/D Navigate   SPACE Select   ESC Back";
    public string verticalKeyboard = "W/S Navigate   SPACE Select   ESC Back";
    public string sliderKeyboard = "A/D Select   W/S Adjust   SPACE Select   ESC Back";
    public string pauseKeyboard = "A/D Navigate   SPACE Select   ESC Resume";

    [Header("Controller Layouts (Dynamic)")]
    string horizontalController;
    string verticalController;
    string sliderController;
    string pauseController;

    public enum MenuType
    {
        Horizontal,
        Vertical,
        Slider,
        Pause
    }

    public MenuType currentMenuType;
    void LateUpdate()
    {
        if (menuCamera == null) return;

        Vector3 forward = menuCamera.transform.forward;
        Vector3 right = menuCamera.transform.right;
        Vector3 up = menuCamera.transform.up;

        float dist = screenOffset.z;

        transform.position =
            menuCamera.transform.position +
            forward * dist +
            right * screenOffset.x * dist +
            up * screenOffset.y * dist;

        transform.rotation =
            Quaternion.LookRotation(transform.position - menuCamera.transform.position);
    }
    void Update()
    {
        DetectInputMode();
        DetectControllerType();
        BuildControllerStrings();
        UpdateFooter();
    }

    // ==================================================
    // INPUT MODE DETECTION
    // ==================================================
    void DetectInputMode()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            currentMode = InputMode.Keyboard;

        if (Gamepad.current != null)
        {
            if (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.01f ||
                Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                currentMode = InputMode.Controller;
            }
        }
    }

    // ==================================================
    // CONTROLLER TYPE DETECTION
    // ==================================================
    void DetectControllerType()
    {
        if (Gamepad.current == null)
        {
            controllerType = ControllerType.Generic;
            return;
        }

        string name = Gamepad.current.name.ToLower();
        string display = Gamepad.current.displayName.ToLower();

        if (name.Contains("dualshock") || name.Contains("dualsense") ||
            display.Contains("playstation"))
        {
            controllerType = ControllerType.PlayStation;
        }
        else if (name.Contains("xinput") || name.Contains("xbox") ||
                 display.Contains("xbox"))
        {
            controllerType = ControllerType.Xbox;
        }
        else
        {
            controllerType = ControllerType.Generic;
        }
    }

    // ==================================================
    // BUILD CONTROLLER TEXT BASED ON TYPE
    // ==================================================
    void BuildControllerStrings()
    {
        string select, back, pause;

        switch (controllerType)
        {
            case ControllerType.PlayStation:
                select = "✕";
                back = "○";
                pause = "Options";
                break;

            case ControllerType.Xbox:
                select = "A";
                back = "B";
                pause = "START";
                break;

            default:
                select = "South";
                back = "East";
                pause = "START";
                break;
        }

        horizontalController = $"LS Navigate   {select} Select   {back} Back";
        verticalController = $"LS Navigate   {select} Select   {back} Back";
        sliderController = $"LS Navigate   {select} Select   {back} Back";
        pauseController = $"LS Navigate   {select} Select   {pause} Resume";
    }

    // ==================================================
    // APPLY TEXT
    // ==================================================
    void UpdateFooter()
    {
        if (footerText == null) return;

        switch (currentMenuType)
        {
            case MenuType.Horizontal:
                footerText.text = currentMode == InputMode.Keyboard ?
                    horizontalKeyboard : horizontalController;
                break;

            case MenuType.Vertical:
                footerText.text = currentMode == InputMode.Keyboard ?
                    verticalKeyboard : verticalController;
                break;

            case MenuType.Slider:
                footerText.text = currentMode == InputMode.Keyboard ?
                    sliderKeyboard : sliderController;
                break;

            case MenuType.Pause:
                footerText.text = currentMode == InputMode.Keyboard ?
                    pauseKeyboard : pauseController;
                break;
        }
    }

    // ==================================================
    // EXTERNAL CALL
    // ==================================================
    public void SetMenuType(MenuType type)
    {
        currentMenuType = type;
    }
}