using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class ESCMenu : MonoBehaviour
{
    public GameObject escMenuUI;
    public static bool isPaused = false;
    public GameObject firstSelectedButton;


    void Start()
    {
        escMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        CursorState.Refresh();
    }

    void Update()
    {
        bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool menuButtonPressed = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

        if (escPressed || menuButtonPressed)
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        escMenuUI.SetActive(false);
        AudioManager.Instance.UnPauseMusic();
        Time.timeScale = 1f;
        isPaused = false;
        EventSystem.current.SetSelectedGameObject(null);
        CursorState.Refresh();
    }

    public void Pause()
    {
        HitStop.Cancel();
        escMenuUI.SetActive(true);
        AudioManager.Instance.PauseMusic();
        Time.timeScale = 0f;
        isPaused = true;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        CursorState.Refresh();
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("MainMenuPitch");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
        Application.Quit();
    }
}
