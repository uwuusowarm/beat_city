using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    private string sceneName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Arcade_Level1_Scene")
        {
            AudioManager.Instance.PlayMusic(MusicType.Stage1);
        }
        else if (sceneName == "Arcade_Level2_Scene")
        {
            AudioManager.Instance.PlayMusic(MusicType.Stage2);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
