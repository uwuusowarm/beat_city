using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UILevelname : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI levelNameTextObject;
    [SerializeField] private GameObject levelNameContainer;
    [SerializeField] private Image levelNameBackground;
    [SerializeField] private float showTime = 2f;
    
    private void Start()
    {
        StartCoroutine(ShowLevelName());
    }

    private IEnumerator ShowLevelName()
    {
        Debug.Log("UILevelname animation started");
        levelNameTextObject.text = SceneManager.GetActiveScene().name;
        
        levelNameContainer.SetActive(true);
        
        Color bgColor = levelNameBackground.color;
        Color textColor = levelNameTextObject.color;
        bgColor.a = 0;
        textColor.a = 0;
        levelNameBackground.color = bgColor;
        levelNameTextObject.color = textColor;

        float duration = 1f;
        float elapsed = 0f;
        float maxAlpha = 0.75f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration) * maxAlpha;
            
            bgColor.a = alpha;
            textColor.a = alpha;
            
            levelNameBackground.color = bgColor;
            levelNameTextObject.color = textColor;
            yield return null;
        }

        Debug.Log("Level name fade-in complete");
        yield return new WaitForSeconds(showTime);

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (elapsed / duration)) * maxAlpha;
            
            bgColor.a = alpha;
            textColor.a = alpha;
            
            levelNameBackground.color = bgColor;
            levelNameTextObject.color = textColor;
            yield return null;
        }

        levelNameContainer.SetActive(false);
        Debug.Log("Level name display complete");
    }
}