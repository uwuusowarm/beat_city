using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; 
public class StoryScreenController : MonoBehaviour
{
    [Header("UI Referenzen")]
    [SerializeField] private GameObject blackBackground;
    [SerializeField] private GameObject storyImageObject;
    [SerializeField] private CanvasGroup imageCanvasGroup;
    [SerializeField] private CanvasGroup textCanvasGroup;

    [Header("Zeiteinstellungen")]
    [SerializeField] private float delayBeforeImage = 1.0f;
    [SerializeField] private float imageFadeDuration = 1.5f;
    [SerializeField] private float delayBeforeText = 1.5f;
    [SerializeField] private float textFadeDuration = 2.5f;

    private bool isSequenceRunning = false;

    void Start()
    {
        if (blackBackground != null) blackBackground.SetActive(false);
        if (storyImageObject != null) storyImageObject.SetActive(false);
        if (textCanvasGroup != null && textCanvasGroup.gameObject != null) textCanvasGroup.gameObject.SetActive(false);
    }

    public void TriggerStorySequence(string sceneToLoad)
    {
        if (!isSequenceRunning)
        {
            StartCoroutine(StorySequenceRoutine(sceneToLoad));
        }
    }

    private IEnumerator StorySequenceRoutine(string sceneToLoad)
    {
        isSequenceRunning = true;

        if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;

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

        isSequenceRunning = false;

        Time.timeScale = 1f; 
        SceneManager.LoadScene(sceneToLoad);
    }
}