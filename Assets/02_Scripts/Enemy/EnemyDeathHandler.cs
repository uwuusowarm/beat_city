using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField] private float despawnDelay = 0.5f;

    private void Awake()
    {
        GetComponent<Health>().OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
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
