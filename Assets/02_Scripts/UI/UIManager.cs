using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIHealth playerHealthUI;
    [SerializeField] private UIHealth enemyHealthUI;
    [SerializeField] private GameObject mobileUIRoot;

    public UIHealth PlayerHealthUI => playerHealthUI;
    public UIHealth EnemyHealthUI => enemyHealthUI;

    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<UIManager>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null) _instance = this;
        
        HandleMobileUI();
    }

    private void HandleMobileUI()
    {
        if (mobileUIRoot == null)
        {
            mobileUIRoot = GameObject.Find("UIMobile");
        }

        if (mobileUIRoot != null)
        {
            bool isMobile = Application.isMobilePlatform;
            
            #if UNITY_EDITOR
            Debug.Log($"[UIManager] Mobile UI {mobileUIRoot.name} is kept active in Editor.");
            #else
            if (!isMobile)
            {
                mobileUIRoot.SetActive(false);
            }
            #endif
        }
    }

    public void UpdateEnemyHealthFocus(Health enemyHealth)
    {
        if (enemyHealthUI != null)
        {
            enemyHealthUI.SetTarget(enemyHealth);
        }
    }
}