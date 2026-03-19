using UnityEngine;

public enum GameState
{
    StartMenu,
    Driving,
    Paused
}

public class GameStateController : MonoBehaviour
{
    public static GameStateController Instance;

    public GameState currentState = GameState.StartMenu;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SetState(GameState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case GameState.Driving:
                Time.timeScale = 1f;
                break;

            case GameState.Paused:
            case GameState.StartMenu:
                Time.timeScale = 0f;
                break;
        }

        // 🔥 HANDLE MENU VISIBILITY CLEANLY
        var menuManager = FindObjectOfType<MenuManager>();

        if (menuManager != null)
        {
            if (newState == GameState.Driving)
            {
                foreach (var m in menuManager.menuControllers)
                    m.gameObject.SetActive(false);
            }
        }
    }
}