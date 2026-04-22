using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using Unity.Cinemachine;

public class SpecialMoveTimeline : MonoBehaviour
{
    [SerializeField] private InputActionReference specialAction;
    [SerializeField] private Meter meter;
    [SerializeField] private int meterCost = 100;
    [SerializeField] private PlayableDirector director;
    [SerializeField] private CinemachineCamera enemyCloseupCam;
    [SerializeField] private float searchRadius = 20f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float visibleRadius = 10f;

    private bool isPlaying;
    private List<GameObject> hiddenEnemies = new List<GameObject>();

    private void OnEnable()
    {
        specialAction.action.performed += OnSpecialInput;
        specialAction.action.Enable();
        director.stopped += OnTimelineFinished;
    }

    private void OnDisable()
    {
        specialAction.action.performed -= OnSpecialInput;
        specialAction.action.Disable();
        director.stopped -= OnTimelineFinished;
    }

    private void OnSpecialInput(InputAction.CallbackContext _)
    {
        if (isPlaying) return;
        if (meter != null && !meter.TrySpend(meterCost)) return;
        
        Transform closest = FindClosestEnemy();
        if (closest != null)
            enemyCloseupCam.Target.TrackingTarget = closest;
        
        HideDistantEnemies();
        
        isPlaying = true;
        Time.timeScale = 0f;
        director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        director.Play();
    }

    private void OnTimelineFinished(PlayableDirector _)
    {
        ShowHiddenEnemies();
        Time.timeScale = 1f;
        isPlaying = false;
    }

    private Transform FindClosestEnemy()
    {
        var hits = Physics.OverlapSphere(
            transform.position, searchRadius, enemyLayer
        );

        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(
                transform.position, hit.transform.position
            );
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit.transform;
            }
        }

        return closest;
    }

    private void HideDistantEnemies()
    {
        hiddenEnemies.Clear();

        var inRange = new HashSet<Collider>(
            Physics.OverlapSphere(
                transform.position, visibleRadius, enemyLayer
            )
        );

        foreach (var enemy in FindObjectsByType<Health>(
            FindObjectsSortMode.None))
        {
            if (enemy.gameObject == gameObject) continue;

            var col = enemy.GetComponent<Collider>();
            if (col != null && !inRange.Contains(col))
            {
                enemy.gameObject.SetActive(false);
                hiddenEnemies.Add(enemy.gameObject);
            }
        }
    }

    private void ShowHiddenEnemies()
    {
        foreach (var enemy in hiddenEnemies)
        {
            if (enemy != null)
                enemy.SetActive(true);
        }
        hiddenEnemies.Clear();
    }
    
    public void DamageAllEnemies()
    {
        foreach (var health in FindObjectsByType<Health>(
            FindObjectsSortMode.None))
        {
            if (health.gameObject == gameObject) continue;
            //hitdata
            health.TakeDamage(new HitData { Damage = 500 });
        }
    }
}