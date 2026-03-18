using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialMenu : MonoBehaviour
{
    public int mainMenuIndex = 0; // set this to your main menu index

    float timer;
    bool canExit;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer > 0.4f)
            canExit = true;

        if (!canExit) return;

        // Keyboard
        if (Keyboard.current.anyKey.wasPressedThisFrame)
            Exit();

        // Controller
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.buttonEast.wasPressedThisFrame ||
                Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.2f)
            {
                Exit();
            }
        }
    }

    void Exit()
    {
        FindObjectOfType<MenuManager>().OpenMenu(mainMenuIndex);
    }
}