using UnityEngine;
using UnityEngine.UI;

// GameOverUI: panel shown when the player's health reaches zero.
// Restart reloads the current level via GameManager.RestartLevel().
// Quit Level returns to the lobby via GameManager.QuitLevel().
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitLevelButton;

    private GameManager gameManager;

    void Awake()
    {
        gameOverPanel.SetActive(false);

        gameManager = FindObjectOfType<GameManager>();

        restartButton.onClick.AddListener(RestartGame);

        quitLevelButton.onClick.RemoveAllListeners();
        quitLevelButton.onClick.AddListener(gameManager.QuitLevel);
    }

    public void ShowGameOver(bool over)
    {
        gameOverPanel.SetActive(over);
    }

    private void RestartGame()
    {
        gameManager.RestartLevel();
    }
}
