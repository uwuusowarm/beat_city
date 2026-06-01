using UnityEngine;
using UnityEngine.SceneManagement;

public class WinAndLose : MonoBehaviour
{
    public GameObject winScreen;    
    void Start()
    {
        winScreen.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            winScreen.SetActive(true);
            Time.timeScale = 0f; 
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
