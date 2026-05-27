using System.Collections.Generic;
using UnityEngine;
using TMPro; 

public class EndlessManager : MonoBehaviour
{
    public static EndlessManager Instance { get; private set; }

    [Header("Level Generation")]
    [SerializeField] private GameObject[] chunkPrefabs; 
    
    [SerializeField] private float chunkWidth = 30f;    
    
    [SerializeField] private int maxActiveChunks = 3;   
    
    [SerializeField] private Transform player;

    [SerializeField] private float startSpawnX = 25f;
    [SerializeField] private float spawnZ = 20f;

    [Header("Score UI (Meter)")]
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI highscoreText;

    private Queue<GameObject> _activeChunks = new Queue<GameObject>();
    private float _spawnX = 0f; 
    private float _startX;      
    private int _highscore;

    private void Awake()
    {
        Instance = this;
        _highscore = PlayerPrefs.GetInt("EndlessHighscore", 0);
    }

    private void Start()
    {
        _spawnX = startSpawnX;

        if (player == null) 
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            _startX = player.position.x;
        }

        UpdateHighscoreUI();

        for (int i = 0; i < maxActiveChunks; i++)
        {
            SpawnNextChunk();
        }
    }

    private void Update()
    {
        if (player == null) return;

        UpdateScore();
        CheckAndGenerateLevel();
    }

    private void UpdateScore()
    {
        int currentMeters = Mathf.Max(0, Mathf.FloorToInt(player.position.x - _startX));

        if (distanceText != null)
            distanceText.text = $"Distance: {currentMeters}m";

        if (currentMeters > _highscore)
        {
            _highscore = currentMeters;
            
            PlayerPrefs.SetInt("EndlessHighscore", _highscore);
            PlayerPrefs.Save(); 
            
            UpdateHighscoreUI();
        }
    }

    private void CheckAndGenerateLevel()
    {
        float triggerPosition = _spawnX - (maxActiveChunks * chunkWidth) + (chunkWidth * 1.5f);

        if (player.position.x > triggerPosition)
        {
            SpawnNextChunk();
            DeleteOldestChunk();
        }
    }

    private void SpawnNextChunk()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0) return;
        int randomIndex = Random.Range(0, chunkPrefabs.Length);
        GameObject chunkToSpawn = chunkPrefabs[randomIndex];
        Quaternion spawnRotation = Quaternion.Euler(0f, 180f, 0f);
        GameObject newChunk = Instantiate(chunkToSpawn, new Vector3(_spawnX, 0f, spawnZ), spawnRotation);
        _activeChunks.Enqueue(newChunk);
        _spawnX += chunkWidth;
    }

    private void DeleteOldestChunk()
    {
        if (_activeChunks.Count > maxActiveChunks)
        {
            GameObject oldChunk = _activeChunks.Dequeue();
            Destroy(oldChunk);
        }
    }

    private void UpdateHighscoreUI()
    {
        if (highscoreText != null)
            highscoreText.text = $"Highscore: {_highscore}m";
    }
}