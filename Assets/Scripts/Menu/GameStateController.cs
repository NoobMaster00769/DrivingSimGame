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
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetState(GameState newState)
    {
        currentState = newState;
    }
}