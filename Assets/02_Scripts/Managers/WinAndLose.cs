using UnityEngine;
using UnityEngine.SceneManagement;

public class WinAndLose : MonoBehaviour
{
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private StoryScreenController storyController;
    [SerializeField] private string nextSceneName = string.Empty;

    void Start()
    {
        if (winScreen != null)
        {
            winScreen.SetActive(false);
        }
        if (loseScreen != null)
        {
            loseScreen.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ShowWinScreen();
        }
    }

    public void ShowWinScreen()
    {
        if (winScreen != null) winScreen.SetActive(true);
        AudioManager.Instance.StopMusic();
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowLoseScreen()
    {
        if (loseScreen != null) loseScreen.SetActive(true);
        AudioManager.Instance.StopMusic();
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ContinueGame()
    {
        if (winScreen != null) winScreen.SetActive(false);
        if (storyController != null)
        {
            storyController.TriggerStorySequence(storyController.EndImageSprite, nextSceneName);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextSceneName);
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