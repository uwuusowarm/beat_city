using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject gameOverPanel;
    
    [Header("Endless Mode UI")]
    [SerializeField] private GameObject endlessScoreContainer;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highscoreText;
    [SerializeField] private GameObject newHighscoreLabel;

    private void OnEnable()
    {
        PlayerDeathHandler.OnPlayerDied += ShowGameOver;
    }

    private void OnDisable()
    {
        PlayerDeathHandler.OnPlayerDied -= ShowGameOver;
    }

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (EndlessManager.Instance != null)
        {
            if (endlessScoreContainer != null) endlessScoreContainer.SetActive(true);

            int currentScore = EndlessManager.Instance.GetCurrentMeters();
            int highscore = EndlessManager.Instance.GetHighscore();
            bool isNewRecord = EndlessManager.Instance.IsNewHighscore;

            if (scoreText != null) scoreText.text = $"Score: {currentScore}m";
            if (highscoreText != null) highscoreText.text = $"Highscore: {highscore}m";
            
            if (newHighscoreLabel != null) newHighscoreLabel.SetActive(isNewRecord);
        }
        else
        {
            if (endlessScoreContainer != null) endlessScoreContainer.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("MainMenuPitch"); 
    }
}