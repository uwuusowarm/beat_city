using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryScreenController : MonoBehaviour
{
    [Header("Autoplay only in Stage 1")]
    [SerializeField] private bool autoPlay;

    [Tooltip("Off: the story track plays through once. On: it repeats until the last image is clicked away.")]
    [SerializeField] private bool loopStoryMusic = false;

    [Header("UI")]
    [SerializeField] private GameObject storyImageObject;
    [SerializeField] private CanvasGroup imageCanvasGroup;
    [SerializeField] private CanvasGroup textCanvasGroup;

    [Header("Story Images (shown in order, click to advance)")]
    [SerializeField] private Sprite[] startImages;
    [SerializeField] private Sprite[] endImages;

    [Header("Fallback if the lists above are empty")]
    [SerializeField] private Image startImage;
    [SerializeField] private Image endImage;

    [Header("Disabled while the story is running")]
    [Tooltip("Drag the player in here. Every script on it and its children gets disabled.")]
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("TimerSettings")]
    [SerializeField] private float delayBeforeImage = 1.0f;
    [SerializeField] private float imageFadeDuration = 1.5f;
    [SerializeField] private float imageSwitchFadeDuration = 0.5f;
    [SerializeField] private float delayBeforeText = 1.5f;
    [SerializeField] private float textFadeDuration = 2.5f;
    [SerializeField] private float endFadeDuration = 1.0f;

    private bool isSequenceRunning = false;
    private Image storyImage;
    private string sceneName;
    private readonly List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();

    private void Awake()
    {
        if (storyImageObject != null)
        {
            storyImage = storyImageObject.GetComponent<Image>();
        }
    }

    private void StretchStoryImageToFullscreen(float safetyMargin = 1f)
    {
        if (storyImage == null) return;

        Canvas canvas = storyImage.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;

        RectTransform canvasRect = canvas.rootCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        RectTransform rect = storyImage.rectTransform;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        Vector3 canvasScale = canvasRect.lossyScale;
        Vector3 parentScale = rect.parent != null ? rect.parent.lossyScale : canvasScale;
        float scaleX = Mathf.Approximately(parentScale.x, 0f) ? 1f : canvasScale.x / parentScale.x;
        float scaleY = Mathf.Approximately(parentScale.y, 0f) ? 1f : canvasScale.y / parentScale.y;

        rect.sizeDelta = new Vector2(canvasRect.rect.width * scaleX, canvasRect.rect.height * scaleY) * safetyMargin;
        rect.position = canvasRect.TransformPoint(canvasRect.rect.center);

        storyImage.type = Image.Type.Simple;
        storyImage.preserveAspect = false;
    }

    private void SetStoryImageTint(float brightness)
    {
        if (storyImage == null) return;
        storyImage.color = new Color(brightness, brightness, brightness, 1f);
    }

    void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;

        if (textCanvasGroup != null && textCanvasGroup.gameObject != null) textCanvasGroup.gameObject.SetActive(false);

        Sprite[] introSprites = autoPlay ? StartImageSprites : new Sprite[0];

        if (introSprites.Length > 0)
        {
            StretchStoryImageToFullscreen(4f);
            SetStoryImageTint(0f);
            if (imageCanvasGroup != null) imageCanvasGroup.alpha = 1f;
            if (storyImageObject != null) storyImageObject.SetActive(true);

            TriggerStorySequence(introSprites);
        }
        else if (storyImageObject != null)
        {
            storyImageObject.SetActive(false);
        }
    }

    public Sprite[] StartImageSprites => ResolveSprites(startImages, startImage);

    public Sprite[] EndImageSprites => ResolveSprites(endImages, endImage);

    private static Sprite[] ResolveSprites(Sprite[] list, Image fallback)
    {
        if (list != null && list.Length > 0) return list;
        if (fallback != null && fallback.sprite != null) return new[] { fallback.sprite };
        return new Sprite[0];
    }

    public void TriggerStorySequence(Sprite spriteToUse = null, string sceneToLoad = null)
    {
        TriggerStorySequence(spriteToUse != null ? new[] { spriteToUse } : null, sceneToLoad);
    }

    public void TriggerStorySequence(Sprite[] spritesToShow, string sceneToLoad = null)
    {
        if (isSequenceRunning) return;

        if (spritesToShow == null || spritesToShow.Length == 0)
        {
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(sceneToLoad);
            }
            return;
        }

        StartCoroutine(StorySequenceRoutine(spritesToShow, sceneToLoad));
    }

    private void DisableGameplayScripts()
    {
        disabledScripts.Clear();

        if (objectsToDisable == null) return;

        foreach (var target in objectsToDisable)
        {
            if (target == null) continue;
            foreach (var script in target.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (script == null || script == this) continue;
                if (!script.enabled) continue;

                script.enabled = false;
                disabledScripts.Add(script);
            }
        }
    }

    private void RestoreGameplayScripts()
    {
        foreach (var script in disabledScripts)
        {
            if (script != null) script.enabled = true;
        }
        disabledScripts.Clear();
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private IEnumerator FadeStoryImageTint(float from, float to, float duration)
    {
        if (storyImage == null) yield break;

        if (duration <= 0f)
        {
            SetStoryImageTint(to);
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            SetStoryImageTint(Mathf.Lerp(from, to, timer / duration));
            yield return null;
        }

        SetStoryImageTint(to);
    }

    /// <summary>
    /// "Any input" has to mean every device the game actually ships on. On
    /// Android there is no Keyboard and no Mouse — only Touchscreen — so
    /// polling just those two left the story unadvanceable on phones.
    /// </summary>
    private IEnumerator WaitForAnyInput()
    {
        while (true)
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) yield break;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) yield break;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) yield break;
            if (Gamepad.current != null &&
                (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                 Gamepad.current.startButton.wasPressedThisFrame)) yield break;

            yield return null;
        }
    }

    private IEnumerator StorySequenceRoutine(Sprite[] sprites, string sceneToLoad)
    {
        isSequenceRunning = true;
        DisableGameplayScripts();

        if (sceneName == "Arcade_Level1_Scene")
        {
            AudioManager.Instance.PlayMusic(MusicType.StorySound1, loopStoryMusic);
        }
        else
        {
            AudioManager.Instance.PlayMusic(MusicType.StorySound2, loopStoryMusic);
        }

        if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;

        if (imageCanvasGroup != null) imageCanvasGroup.alpha = 1f;
        SetStoryImageTint(0f);
        StretchStoryImageToFullscreen(4f);
        if (storyImageObject != null) storyImageObject.SetActive(true);

        yield return new WaitForSecondsRealtime(delayBeforeImage);

        if (storyImage != null) storyImage.sprite = sprites[0];
        StretchStoryImageToFullscreen();

        yield return FadeStoryImageTint(0f, 1f, imageFadeDuration);

        yield return new WaitForSecondsRealtime(delayBeforeText);

        if (textCanvasGroup != null && textCanvasGroup.gameObject != null) textCanvasGroup.gameObject.SetActive(true);

        yield return FadeCanvasGroup(textCanvasGroup, 0f, 1f, textFadeDuration);

        for (int i = 0; i < sprites.Length; i++)
        {
            yield return WaitForAnyInput();

            bool isLast = i == sprites.Length - 1;
            if (isLast) break;

            yield return FadeStoryImageTint(1f, 0f, imageSwitchFadeDuration);
            if (storyImage != null) storyImage.sprite = sprites[i + 1];
            StretchStoryImageToFullscreen();
            yield return FadeStoryImageTint(0f, 1f, imageSwitchFadeDuration);
        }

        Coroutine textFadeOut = StartCoroutine(FadeCanvasGroup(textCanvasGroup, 1f, 0f, endFadeDuration));
        yield return FadeStoryImageTint(1f, 0f, endFadeDuration);
        yield return textFadeOut;

        isSequenceRunning = false;

        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
            yield break;
        }

        if (sceneName == "Arcade_Level1_Scene")
        {
            AudioManager.Instance.PlayMusic(MusicType.Stage1);
        }

        yield return FadeCanvasGroup(imageCanvasGroup, 1f, 0f, endFadeDuration);

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

        RestoreGameplayScripts();
    }
}
