using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // Wichtig fürs Szenenladen!

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

    // Erwartet jetzt den Namen der nächsten Szene
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

        // STEP 1: Schwarzer Hintergrund knallt sofort rein
        if (blackBackground != null) blackBackground.SetActive(true);

        // STEP 2: Delay vor dem Bild
        yield return new WaitForSecondsRealtime(delayBeforeImage);

        // STEP 3: Story-Bild aktivieren und einfaden
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

        // STEP 4: Atempause vor dem Text
        yield return new WaitForSecondsRealtime(delayBeforeText);

        // STEP 5: Text aktivieren und einfaden
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

        // STEP 6: Warten auf Tastendruck/Mausklick
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

        // STEP 7: Alles ausmachen
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

        // STEP 8: Zeit zurücksetzen und nächste Szene laden!
        Time.timeScale = 1f; // Ganz wichtig, damit das neue Level nicht pausiert startet!
        SceneManager.LoadScene(sceneToLoad);
    }
}