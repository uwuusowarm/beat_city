using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        AudioManager.Instance.PlayMusic(MusicType.Stage1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
