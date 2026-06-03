using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] scriptsToDisable; 
    [SerializeField] private Animator animator;

    public delegate void PlayerDiedAction();
    public static event PlayerDiedAction OnPlayerDied;

    private void Awake()
    {
        GetComponent<Health>().OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {

        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }

        if (animator != null) animator.SetTrigger("Death");
        OnPlayerDied?.Invoke();
    }
}