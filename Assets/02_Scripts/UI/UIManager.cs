using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIHealth playerHealthUI;
    [SerializeField] private UIHealth enemyHealthUI;

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
    }

    public void UpdateEnemyHealthFocus(Health enemyHealth)
    {
        if (enemyHealthUI != null)
        {
            enemyHealthUI.SetTarget(enemyHealth);
        }
    }
}