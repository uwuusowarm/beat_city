using UnityEngine;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [SerializeField] private Difficulty currentDifficulty = Difficulty.Normal;

    public Difficulty CurrentDifficulty => currentDifficulty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetEasy()
    {
        currentDifficulty = Difficulty.Easy;
    }

    public void SetNormal()
    {
        currentDifficulty = Difficulty.Normal;
    }

    public void SetHard()
    {
        currentDifficulty = Difficulty.Hard;
    }

    public float GetHealthMultiplier()
    {
        return currentDifficulty switch
        {
            Difficulty.Easy => 0.7f,
            Difficulty.Normal => 1f,
            Difficulty.Hard => 1.3f,
            _ => 1f
        };
    }

    public float GetDamageMultiplier()
    {
        return currentDifficulty switch
        {
            Difficulty.Easy => 0.75f,
            Difficulty.Normal => 1f,
            Difficulty.Hard => 1.25f,
            _ => 1f
        };
    }
}