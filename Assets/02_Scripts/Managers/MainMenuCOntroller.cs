using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuCOntroller : MonoBehaviour
{
    public GameObject firstSelectedButton;
    
    void Start()
    {
        Time.timeScale = 1f;
        if (firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }
}
