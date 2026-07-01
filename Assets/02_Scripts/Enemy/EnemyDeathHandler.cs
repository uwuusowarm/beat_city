using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField] private float despawnDelay = 0.5f;
    [SerializeField] private int coinReward = 10;

    private void Awake()
    {
        GetComponent<Health>().OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddCoins(coinReward);
        }
        
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        foreach (var c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        StartCoroutine(Despawn());
    }

    private IEnumerator Despawn()
    {
        yield return new WaitForSeconds(despawnDelay);
        Destroy(gameObject);
    }
}
