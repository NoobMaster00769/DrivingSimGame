using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialMenu : MonoBehaviour
{
    public int mainMenuIndex = 0; 

    float timer;
    bool canExit;

    void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer > 0.4f)
            canExit = true;

        if (!canExit) return;


        if (Keyboard.current.anyKey.wasPressedThisFrame)
            Exit();


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