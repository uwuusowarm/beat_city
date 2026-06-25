using System.Collections;
using TMPro;
using UnityEngine;

public class LevelTitleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private float fadeInTime = 1f;
    [SerializeField] private float visibleTime = 2f;
    [SerializeField] private float fadeOutTime = 1f;

    private void Start()
    {
        StartCoroutine(ShowLevelTitle());
    }

    private IEnumerator ShowLevelTitle()
    {
        Color color = levelText.color;
        color.a = 0;
        levelText.color = color;

        float timer = 0;

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            color.a = timer / fadeInTime;
            levelText.color = color;
            yield return null;
        }

        color.a = 1;
        levelText.color = color;

        yield return new WaitForSeconds(visibleTime);

        timer = 0;

        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            color.a = 1 - (timer / fadeOutTime);
            levelText.color = color;
            yield return null;
        }

        color.a = 0;
        levelText.color = color;
    }
}