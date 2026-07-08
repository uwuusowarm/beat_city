using UnityEngine;
using UnityEngine.Playables;

public class UltimateCutsceneManager : MonoBehaviour
{
    [System.Serializable]
    public class UltimateCutscene
    {
        public string ultimateName;
        public PlayableDirector director;
    }

    [SerializeField]public CameraFollow cameraFollow;
    [SerializeField]public PlayerBoundsConstraint playerBounds;
    [SerializeField] private UltimateCutscene[] cutscenes;
    
    public void PlayCutscene(string ultimateName)
    {
        foreach (var cutscene in cutscenes)
        {
            if (cutscene.ultimateName == ultimateName)
            {
                cameraFollow.enabled = false;
                playerBounds.enabled = false;
                cutscene.director.stopped += OnCutsceneStopped;
                cutscene.director.Play();
                return;
            }
        }
        Debug.LogWarning($"Add a new cutscene, bozo: '{ultimateName}'");
    }

    private void OnCutsceneStopped(PlayableDirector director)
    {
        director.stopped -= OnCutsceneStopped;
        cameraFollow.enabled = true;
        playerBounds.enabled = true;
    }

    public void FollowCameraToggle()
    {
        if (cameraFollow.enabled)
        {
            cameraFollow.enabled = false;
            playerBounds.enabled = false;
        }
        else
        {
            cameraFollow.enabled = true;
            playerBounds.enabled = true;
        }
    }
}