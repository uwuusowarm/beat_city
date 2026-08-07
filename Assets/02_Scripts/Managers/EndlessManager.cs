using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EndlessManager : MonoBehaviour
{
    public static EndlessManager Instance { get; private set; }

    [System.Serializable]
    public class TileSet
    {
        public string name;
        public GameObject[] chunkPrefabs;
        public GameObject transitionPrefab;
        public int minChunksBeforeTransition = 5;
        [Range(0f, 1f)] public float transitionChance = 0.3f;
    }

    [Header("Level Generation")]
    [SerializeField] private TileSet[] tileSets;

    [SerializeField] private float chunkWidth = 30f;
    [SerializeField] private float spawnAheadDistance = 75f;
    [SerializeField] private float despawnDistance = 100f;

    [SerializeField] private int maxActiveChunks = 3;

    [SerializeField] private Transform player;

    [SerializeField] private float startSpawnX = 25f;
    [SerializeField] private float spawnZ = 20f;

    [Header("Score UI (Meter)")]
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI highscoreText;
    public bool IsNewHighscore { get; private set; }

    private Queue<GameObject> _activeChunks = new Queue<GameObject>();
    private float _spawnX = 0f;
    private float _startX;
    private int _highscore;
    private int _currentSetIndex;
    private int _chunksInCurrentSet;

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

        _spawnX = startSpawnX - chunkWidth;

        SpawnNextChunk();

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
            IsNewHighscore = true;

            PlayerPrefs.SetInt("EndlessHighscore", _highscore);
            PlayerPrefs.Save();

            UpdateHighscoreUI();
        }
    }

    private void CheckAndGenerateLevel()
    {
        float triggerPosition = _spawnX - spawnAheadDistance;

        if (player.position.x > triggerPosition)
        {
            SpawnNextChunk();
            DeleteOldestChunk();
        }
    }

    private void SpawnNextChunk()
    {
        if (tileSets == null || tileSets.Length == 0) return;

        TileSet currentSet = tileSets[_currentSetIndex];
        GameObject chunkToSpawn;

        bool doTransition = tileSets.Length > 1
            && currentSet.transitionPrefab != null
            && _chunksInCurrentSet >= currentSet.minChunksBeforeTransition
            && Random.value < currentSet.transitionChance;

        if (doTransition)
        {
            chunkToSpawn = currentSet.transitionPrefab;
            _currentSetIndex = (_currentSetIndex + 1) % tileSets.Length;
            _chunksInCurrentSet = 0;
        }
        else
        {
            if (currentSet.chunkPrefabs == null || currentSet.chunkPrefabs.Length == 0) return;
            int randomIndex = Random.Range(0, currentSet.chunkPrefabs.Length);
            chunkToSpawn = currentSet.chunkPrefabs[randomIndex];
            _chunksInCurrentSet++;
        }

        Quaternion spawnRotation = Quaternion.Euler(0f, 180f, 0f);
        GameObject newChunk = Instantiate(chunkToSpawn, new Vector3(_spawnX, 0f, spawnZ), spawnRotation);
        _activeChunks.Enqueue(newChunk);
        float width = chunkWidth;
        if (newChunk.TryGetComponent<ChunkWidth>(out var cw)) width = cw.width;
        _spawnX += width;
    }

    private void DeleteOldestChunk()
    {
        while (_activeChunks.Count > maxActiveChunks && _activeChunks.Peek().transform.position.x < player.position.x - despawnDistance)
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

    public int GetCurrentMeters() => Mathf.Max(0, Mathf.FloorToInt(player.position.x - _startX));
    public int GetHighscore() => _highscore;
}