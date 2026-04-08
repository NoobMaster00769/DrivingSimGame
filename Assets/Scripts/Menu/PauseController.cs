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


        if (start)
        {
            if (state == GameState.Driving)
                EnterPause();
            else if (state == GameState.Paused)
                Resume();

            return;
        }


        if (!esc) return;

        if (state == GameState.Driving)
        {
            EnterPause();
        }
        else if (state == GameState.Paused)
        {

            if (IsPauseMenuActive())
            {
                Resume();
            }
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
        if (GameStateController.Instance.currentState == GameState.Paused)
            return;

        cameraDirector.SwitchToMenu();

        menuManager.OpenMenuInstant(pauseMenuIndex);

        menuManager.menuControllers[pauseMenuIndex].enabled = true;

        StartCoroutine(SetPausedNextFrame());
    }

    IEnumerator SetPausedNextFrame()
    {
        yield return null;
        GameStateController.Instance.SetState(GameState.Paused);
    }

    public void Resume()
    {

        cameraDirector.SwitchToGameplay();


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


        yield return ScreenFader.Instance.FadeOut(0.5f);


        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}