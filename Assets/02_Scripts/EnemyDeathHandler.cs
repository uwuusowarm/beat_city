using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField] private float despawnDelay = 0.5f;

    private void Awake()
    {
        GetComponent<Health>().OnDeath += () => StartCoroutine(Despawn());
    }

    private IEnumerator Despawn()
    {
        yield return new WaitForSeconds(despawnDelay);
        Destroy(gameObject);
    }
}
