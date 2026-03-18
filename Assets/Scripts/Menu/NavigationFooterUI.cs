using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class NavigationFooterUI : MonoBehaviour
{
    public TMP_Text footerText;

    enum InputMode { Keyboard, Controller }
    InputMode currentMode;

    [Header("Keyboard Layouts")]
    public string horizontalKeyboard = "A/D Navigate   SPACE Select   ESC Back";
    public string verticalKeyboard = "W/S Navigate   SPACE Select   ESC Back";
    public string sliderKeyboard = "A/D Select   W/S Adjust   SPACE Select   ESC Back";

    [Header("Controller Layouts")]
    public string horizontalController = "LS Navigate   A Select   B Back";
    public string verticalController = "LS Navigate   A Select   B Back";
    public string sliderController = "LS Navigate   A Select   B Back";

    public enum MenuType
    {
        Horizontal,
        Vertical,
        Slider
    }

    public MenuType currentMenuType;

    void Update()
    {
        DetectInputMode();
        UpdateFooter();
    }

    void DetectInputMode()
    {
        if (Keyboard.current.anyKey.wasPressedThisFrame)
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
        }
    }

    // 🔥 Call this from menus
    public void SetMenuType(MenuType type)
    {
        currentMenuType = type;
    }
}