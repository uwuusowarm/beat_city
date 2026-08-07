using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryScreenController : MonoBehaviour
{
    [Header("Autoplay only in Stage 1")]
    [SerializeField] private bool autoPlay;

    [Header("UI")]
    [SerializeField] private GameObject blackBackground;
    [SerializeField] private GameObject storyImageObject;
    [SerializeField] private CanvasGroup imageCanvasGroup;
    [SerializeField] private CanvasGroup textCanvasGroup;
    [SerializeField] private Image startImage;
    [SerializeField] private Image endImage;

    [Header("TimerSettings")]
    [SerializeField] private float delayBeforeImage = 1.0f;
    [SerializeField] private float imageFadeDuration = 1.5f;
    [SerializeField] private float delayBeforeText = 1.5f;
    [SerializeField] private float textFadeDuration = 2.5f;

    private bool isSequenceRunning = false;
    private Image storyImage;
    private string sceneName;

    private void Awake()
    {
        if (storyImageObject != null)
        {
            storyImage = storyImageObject.GetComponent<Image>();
        }
    }

    void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;

        if (blackBackground != null) blackBackground.SetActive(false);
        if (storyImageObject != null) storyImageObject.SetActive(false);
        if (textCanvasGroup != null && textCanvasGroup.gameObject != null) textCanvasGroup.gameObject.SetActive(false);

        if (autoPlay)
        {
            TriggerStorySequence(startImage.sprite);
        }
    }

    public void TriggerStorySequence(Sprite spriteToUse = null, string sceneToLoad = null)
    {
        if (!isSequenceRunning)
        {
            if (storyImage != null && spriteToUse != null)
            {
                storyImage.sprite = spriteToUse;
            }
            StartCoroutine(StorySequenceRoutine(sceneToLoad));
        }
    }

    public Sprite EndImageSprite => endImage != null ? endImage.sprite : null;

    private IEnumerator StorySequenceRoutine(string sceneToLoad)
    {
        isSequenceRunning = true;

        if (sceneName == "Arcade_Level1_Scene")
        {
            AudioManager.Instance.PlayMusic(MusicType.StorySound1);
        }
        else
        {
            AudioManager.Instance.PlayMusic(MusicType.StorySound2);
        }

        if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;

        if (imageCanvasGroup != null) imageCanvasGroup.alpha = 0f;

        if (blackBackground != null) blackBackground.SetActive(true);

        yield return new WaitForSecondsRealtime(delayBeforeImage);

        if (storyImageObject != null) storyImageObject.SetActive(true);

        float timer = 0f;
        if (imageCanvasGroup != null)
        {
            while (timer < imageFadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                imageCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / imageFadeDuration);
                yield return null;
            }
            imageCanvasGroup.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(delayBeforeText);

        if (textCanvasGroup != null && textCanvasGroup.gameObject != null) textCanvasGroup.gameObject.SetActive(true);

        timer = 0f;
        if (textCanvasGroup != null)
        {
            while (timer < textFadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                textCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / textFadeDuration);
                yield return null;
            }
            textCanvasGroup.alpha = 1f;
        }

        bool inputDetected = false;
        while (!inputDetected)
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                inputDetected = true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                inputDetected = true;
            }

            yield return null;
        }

        


        isSequenceRunning = false;

        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else if (sceneName == "Arcade_Level1_Scene")
        {
            AudioManager.Instance.PlayMusic(MusicType.Stage1);
            if (blackBackground != null) blackBackground.SetActive(false);
            if (storyImageObject != null)
            {
                if (imageCanvasGroup != null) imageCanvasGroup.alpha = 0f;
                storyImageObject.SetActive(false);
            }
            if (textCanvasGroup != null && textCanvasGroup.gameObject != null)
            {
                textCanvasGroup.alpha = 0f;
                textCanvasGroup.gameObject.SetActive(false);
            }
        }

    }
}