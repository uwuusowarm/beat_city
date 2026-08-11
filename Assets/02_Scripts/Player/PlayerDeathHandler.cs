using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [SerializeField] private Animator animator;
    [SerializeField] private WinAndLose WinLoseScript;

    [Header("Death Sequence")]
    [Tooltip("Wartezeit nach dem Death-Trigger, bevor Lose-Screen und Pause kommen. Auf die Laenge des Death-Clips setzen.")]
    [SerializeField] private float deathScreenDelay = 1.0f;

    public delegate void PlayerDiedAction();
    public static event PlayerDiedAction OnPlayerDied;

    private Health _health;
    private bool _isDead;
    private bool _sequenceFinished;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_health != null) _health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (_isDead) return;
        _isDead = true;

        if (PlayerStateManager.Instance != null)
            PlayerStateManager.Instance.SetState(PlayerState.Dead);

        foreach (var box in GetComponentsInChildren<Hitbox>(true))
        {
            if (box != null) box.Deactivate();
        }

        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }

        PlayDeathAnimation();

        StartCoroutine(ShowLoseScreenDelayed());
    }

    private void PlayDeathAnimation()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hit");
        animator.SetFloat("Speed", 0f);
        animator.SetBool("IsGrabbing", false);
        animator.SetTrigger("Death");
    }

    private IEnumerator ShowLoseScreenDelayed()
    {
        yield return new WaitForSecondsRealtime(deathScreenDelay);

        FinishDeathSequence();
    }

    public void FinishDeathSequence()
    {
        if (_sequenceFinished) return;
        _sequenceFinished = true;

        if (WinLoseScript != null)
        {
            WinLoseScript.ShowLoseScreen();
        }

        OnPlayerDied?.Invoke();
    }
}
