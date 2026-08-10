using System.Collections;
using UnityEngine;

public class SpecialMove : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private InputBuffer inputBuffer;
    [SerializeField] private Animator animator;
    [SerializeField] private AfterimageEffect afterimageEffect;
    [SerializeField] private Health playerHealth;

    private PlayerSettings _settings;
    private float _originalAnimSpeed;
    private bool _animSpeedOverridden;
    private bool _isPerforming;
    private bool _hitFinished;
    private bool _cancelled;

    public bool IsPerforming => _isPerforming;

    private void Awake()
    {
        _settings = SettingsResolver.ResolvePlayerSettings();

        if (playerHealth == null)
            playerHealth = GetComponent<Health>();

        if (_settings == null)
        {
            Debug.LogWarning("[SpecialMove] No PlayerSettings found (provider/resources).");
        }
    }

    private void OnDisable()
    {
        if (!_isPerforming) return;

        RestoreVisuals();

        _isPerforming = false;
        _cancelled = false;

        if (playerCombat != null)
            playerCombat.AbortSpecialChain(false);
    }

    public bool TryActivate()
    {
        if (_isPerforming) return false;

        _isPerforming = true;
        StartCoroutine(SpecialSequence());
        return true;
    }

    private IEnumerator SpecialSequence()
    {
        playerCombat.SetSpecialChainActive(true);

        inputBuffer.Clear();

        _cancelled = false;
        CombatInputType chainType = CombatInputType.None;

        while (chainType == CombatInputType.None)
        {
            if (ShouldCancel())
            {
                inputBuffer.DiscardSpecialInputs();

                _isPerforming = false;
                playerCombat.AbortSpecialChain(true);
                yield break;
            }

            inputBuffer.DiscardSpecialInputs();

            if (PlayerStateManager.Instance.CanPerformAction() && inputBuffer.TryConsume(out var input))
            {
                if (input == CombatInputType.Punch || input == CombatInputType.Kick)
                    chainType = input;
            }

            yield return null;
        }

        PlayerStateManager.Instance.SetState(PlayerState.SpecialAttacking);

        bool useAfterimage = _settings == null || _settings.specialChainAfterimage;
        if (afterimageEffect != null && useAfterimage)
            afterimageEffect.Activate();

        float animSpeed = _settings != null ? _settings.specialAnimSpeed : 1.5f;
        if (animator != null)
        {
            _originalAnimSpeed = animator.speed;
            _animSpeedOverridden = true;
            animator.speed = animSpeed;
        }

        yield return DoChain(chainType);

        RestoreVisuals();
        inputBuffer.DiscardSpecialInputs();

        _isPerforming = false;

        if (_cancelled)
        {
            _cancelled = false;
            playerCombat.AbortSpecialChain(false);
            yield break;
        }

        playerCombat.SetSpecialChainActive(false);
        playerCombat.FinishAttack();
    }

    private IEnumerator DoChain(CombatInputType chainType)
    {
        int count = chainType == CombatInputType.Punch
            ? (_settings != null ? _settings.specialPunchCount : 3)
            : (_settings != null ? _settings.specialKickCount : 3);

        int maxComboSteps = _settings != null ? _settings.maxComboSteps : 3;

        for (int i = 0; i < count; i++)
        {
            bool isLast = i == count - 1;
            ConfigureHitbox(chainType, isLast);

            int comboStep = isLast ? maxComboSteps : (i % (maxComboSteps - 1)) + 1;

            if (playerCombat != null)
                playerCombat.SetSpecialChainAttack(chainType, comboStep);

            if (animator != null)
            {
                animator.SetInteger("ComboStep", comboStep);
                animator.SetInteger("AttackType", (int)chainType);
                animator.SetBool("IsSpecial", true);
                animator.SetTrigger("Attack");
            }

            _hitFinished = false;
            while (!_hitFinished)
            {
                if (ShouldCancel())
                {
                    _cancelled = true;
                    yield break;
                }

                inputBuffer.DiscardSpecialInputs();
                yield return null;
            }
        }
    }

    private bool ShouldCancel()
    {
        if (playerHealth != null && playerHealth.Current <= 0)
            return true;

        if (PlayerStateManager.Instance != null &&
            PlayerStateManager.Instance.CurrentState == PlayerState.Stunned)
            return true;

        return false;
    }

    private void RestoreVisuals()
    {
        if (animator != null)
        {
            if (_animSpeedOverridden)
                animator.speed = _originalAnimSpeed;

            animator.SetBool("IsSpecial", false);
        }

        _animSpeedOverridden = false;

        if (afterimageEffect != null)
            afterimageEffect.Deactivate();
    }

    private void ConfigureHitbox(CombatInputType chainType, bool isLast)
    {
        if (hitbox == null || _settings == null) return;

        hitbox.HitStopDuration = _settings.combatHitStop;
        hitbox.JugglingForce = _settings.jugglingForce;

        if (chainType == CombatInputType.Punch)
        {
            hitbox.Damage = _settings.specialPunchDamage;
            hitbox.KnockbackForce = _settings.punchBaseKnockback;
            hitbox.KnockUpForce = isLast ? _settings.specialFinisherKnockup : 0f;
            hitbox.IsLauncher = isLast;
            hitbox.ShouldKnockdown = false;
        }
        else
        {
            hitbox.Damage = _settings.specialKickDamage;
            hitbox.KnockbackForce = isLast ? _settings.specialFinisherKnockback : _settings.kickBaseKnockback;
            hitbox.KnockUpForce = 0f;
            hitbox.IsLauncher = false;
            hitbox.ShouldKnockdown = isLast;
        }
    }

    public void OnChainHitFinished()
    {
        _hitFinished = true;
    }
}
