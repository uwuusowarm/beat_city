using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] scriptsToDisable; 
    [SerializeField] private Animator animator;
    [SerializeField] private WinAndLose WinLoseScript;

    public delegate void PlayerDiedAction();
    public static event PlayerDiedAction OnPlayerDied;

    private void Awake()
    {
        GetComponent<Health>().OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        WinLoseScript.ShowLoseScreen();

        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }

        if (animator != null) animator.SetTrigger("Death");
        OnPlayerDied?.Invoke();
    }
}