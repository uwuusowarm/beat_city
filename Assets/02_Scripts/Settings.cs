using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class Settings : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    Resolution[] resolutions;

    public TMP_Dropdown fpsDropdown;
    private int[] fpsOptions = { 30, 60, 120, 165 };
    private bool vsyncEnabled = true;
    public GameObject firstSelectedButton;


    void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        
        resolutions = Screen.resolutions.Select(res => new Resolution { width = res.width, height = res.height }).Distinct().ToArray();
        resolutionDropdown.ClearOptions();

        var options = resolutions.Select(r => r.width + " x " + r.height).ToList();
        resolutionDropdown.AddOptions(options);
        int currentIndex = resolutions.ToList().FindIndex(r => r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();

        SetVSync(true);
    }

    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void SetVSync(bool enabled)
    {
        vsyncEnabled = enabled;
        QualitySettings.vSyncCount = enabled ? 1 : 0;

        fpsDropdown.interactable = !enabled;
        if (enabled)
        {
            Application.targetFrameRate = -1;
        }
        else
        {
            SetMaxFPS(fpsDropdown.value);
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetMaxFPS(int index)
    {
        if (vsyncEnabled)
        {
            Application.targetFrameRate = -1;
            return;
        }

        int fps = fpsOptions[index];
        Application.targetFrameRate = fps;
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
        Application.Quit();
    }

    public void Play()
    {
        SceneManager.LoadScene("Alihan_Scene");
    }


}
