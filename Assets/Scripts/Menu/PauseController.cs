using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

public class PauseSystem : MonoBehaviour
{
    public CameraDirector cameraDirector;
    public MenuManager menuManager;

    public int pauseMenuIndex = 7;

    void Update()
    {
        var state = GameStateController.Instance.currentState;

        bool esc =
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;

        bool start =
            Gamepad.current != null &&
            Gamepad.current.startButton.wasPressedThisFrame;

        // ----------------------------------
        // 🎮 START = ALWAYS TOGGLE
        // ----------------------------------
        if (start)
        {
            if (state == GameState.Driving)
                EnterPause();
            else if (state == GameState.Paused)
                Resume();

            return;
        }

        // ----------------------------------
        // ⌨️ ESC LOGIC (SMART)
        // ----------------------------------
        if (!esc) return;

        if (state == GameState.Driving)
        {
            EnterPause();
        }
        else if (state == GameState.Paused)
        {
            // 🔥 ONLY resume if PAUSE MENU is active
            if (IsPauseMenuActive())
            {
                Resume();
            }
            // ❌ otherwise do nothing → menus handle ESC as back
        }
    }

    bool IsPauseMenuActive()
    {
        if (pauseMenuIndex >= menuManager.menuControllers.Length)
            return false;

        return menuManager.menuControllers[pauseMenuIndex].gameObject.activeSelf;
    }

    void EnterPause()
    {
        // prevent double triggering
        if (GameStateController.Instance.currentState == GameState.Paused)
            return;

        // camera first
        cameraDirector.SwitchToMenu();

        // open pause menu instantly
        menuManager.OpenMenuInstant(pauseMenuIndex);

        // ensure controller is active
        menuManager.menuControllers[pauseMenuIndex].enabled = true;

        // delay state to avoid race condition
        StartCoroutine(SetPausedNextFrame());
    }

    IEnumerator SetPausedNextFrame()
    {
        yield return null;
        GameStateController.Instance.SetState(GameState.Paused);
    }

    public void Resume()
    {
        // camera first
        cameraDirector.SwitchToGameplay();

        // 🔥 disable ONLY current menu
        if (pauseMenuIndex < menuManager.menuControllers.Length)
            menuManager.menuControllers[pauseMenuIndex].gameObject.SetActive(false);

        GameStateController.Instance.SetState(GameState.Driving);
    }

    public void ExitToMainMenu()
    {
        StartCoroutine(ExitRoutine());
    }
    IEnumerator ExitRoutine()
    {
        Time.timeScale = 1f;

        // 🔥 Fade to black
        yield return ScreenFader.Instance.FadeOut(0.5f);

        // 🔥 Reset scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}