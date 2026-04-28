using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Central game manager handling game states, scoring, and restart logic.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, Paused, Dead, Won }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    public float playTime;
    public int deaths;

    private GameUI gameUI;
    private string messageToShow;
    private float messageTimer;

    public System.Action<GameState> OnGameStateChanged;
    public System.Action<string> OnMessageDisplay;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        gameUI = FindFirstObjectByType<GameUI>();
        CurrentState = GameState.Playing;
    }

    void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            playTime += Time.deltaTime;
        }

        // Pause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (CurrentState == GameState.Playing)
                PauseGame();
            else if (CurrentState == GameState.Paused)
                ResumeGame();
        }

        // Message timer
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
        }
    }

    public void PauseGame()
    {
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void ResumeGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        bool useMouseLook = playerController != null && playerController.useMouseLook;
        Cursor.lockState = useMouseLook ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !useMouseLook;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void OnPlayerDied()
    {
        CurrentState = GameState.Dead;
        deaths++;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void OnPlayerEscaped()
    {
        CurrentState = GameState.Won;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void OnAllTrinketsFound()
    {
        ShowMessage("14 trinkets collected! You escaped the maze!");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ShowMessage(string msg)
    {
        messageToShow = msg;
        messageTimer = 4f;
        OnMessageDisplay?.Invoke(msg);
    }

    public string GetCurrentMessage()
    {
        return messageTimer > 0 ? messageToShow : null;
    }

    public float GetMessageAlpha()
    {
        if (messageTimer <= 0) return 0f;
        if (messageTimer > 3f) return (4f - messageTimer);
        if (messageTimer < 1f) return messageTimer;
        return 1f;
    }
}
