using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIHealth playerHealthUI;
    [SerializeField] private UIHealth enemyHealthUI;

    public UIHealth PlayerHealthUI => playerHealthUI;

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
}