using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoader : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset arcadeScene;
    [SerializeField] private SceneAsset endlessScene;
#endif

    [SerializeField, HideInInspector]
    private string sceneNameArcade;

    [SerializeField, HideInInspector]
    private string sceneNameEndless;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (arcadeScene != null)
            sceneNameArcade = arcadeScene.name;
        if (endlessScene != null)
        {
            sceneNameEndless = endlessScene.name;
        }
    }
#endif
    
    public void LoadArcadeScene()
    {
        SceneManager.LoadScene(sceneNameArcade);
    }

    public void LoadEndlessScene()
    {
        SceneManager.LoadScene(sceneNameEndless);
    }
}
