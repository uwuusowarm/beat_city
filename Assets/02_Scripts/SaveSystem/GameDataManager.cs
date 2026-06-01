using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    private SaveData data;

    public int PrestigePoints => data.prestigePoints;
    
    public event Action OnDataChanged;

    private const string MAIN_MENU_SCENE = "MainMenu"; //TODO: when we have a name for our mainmenu scene, put it here

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        data = SaveSystem.Load();
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SaveAndGoToMenu();
    }
    
    public void AddPrestigePoints(int amount)
    {
        data.prestigePoints += amount;
        OnDataChanged?.Invoke();
    }
    
    public bool TrySpendPrestigePoints(int cost)
    {
        if (cost < 0 || data.prestigePoints < cost) return false;
        data.prestigePoints -= cost;
        Save();
        OnDataChanged?.Invoke();
        return true;
    }

    public void Save()     => SaveSystem.Save(data);

    public void ResetAll()
    {
        SaveSystem.Delete();
        data = new SaveData();
        OnDataChanged?.Invoke();
    }

    public void SaveAndGoToMenu()
    {
        Save();
        SceneManager.LoadScene(MAIN_MENU_SCENE);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == MAIN_MENU_SCENE) Save();
    }

    private void OnApplicationQuit() => Save();
    
    private void OnApplicationPause(bool isPaused) //this fires when app goes in background on phone
    {
        if (isPaused) Save();
    }
}