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
    string horizontalKeyboard = "A/D Navigate\nSPACE / ENTER Select\nESC Back";
    string verticalKeyboard = "W/S Navigate\nSPACE / ENTER Select\nESC Back";
    string sliderKeyboard = "A/D Select\nW/S Adjust\nSPACE / ENTER Select\nESC Back";
    string pauseKeyboard = "A/D Navigate\nSPACE / ENTER Select\nESC Resume";

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

        horizontalController = $"LS Navigate\n{select} Select\n{back} Back";
        verticalController = $"LS Navigate\n{select} Select\n{back} Back";
        sliderController = $"LS Navigate\n{select} Select\n{back} Back";
        pauseController = $"LS Navigate\n{select} Select\n{pause} Resume";
    }


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


    public void SetMenuType(MenuType type)
    {
        currentMenuType = type;
    }
}