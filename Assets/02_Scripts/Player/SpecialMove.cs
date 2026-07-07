using System.Collections;
using UnityEngine;

public class SpecialMove : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private InputBuffer inputBuffer;
    [SerializeField] private Animator animator;
    [SerializeField] private Meter meter;
    [SerializeField] private AfterimageEffect afterimageEffect;

    private PlayerSettings _settings;
    private float _originalAnimSpeed;
    private bool _isPerforming;
    private bool _hitFinished;

    public bool IsPerforming => _isPerforming;

    private void Awake()
    {
        _settings = Resources.Load<PlayerSettings>("PlayerSettings");
    }
    
    public bool TryActivate()
    {
        if (_isPerforming) return false;
        if (meter == null) return false;

        int cost = _settings != null ? Mathf.RoundToInt(_settings.specialMeterCost) : 50;
        if (!meter.TrySpend(cost)) return false;

        _isPerforming = true;
        StartCoroutine(SpecialSequence());
        return true;
    }

    private IEnumerator SpecialSequence()
    {
        PlayerStateManager.Instance.SetState(PlayerState.SpecialAttacking);
        playerCombat.SetSpecialChainActive(true);

        inputBuffer.Clear();

        float window = _settings != null ? _settings.specialInputWindow : 1f;
        float timer = 0f;
        CombatInputType chainType = CombatInputType.None;

        while (chainType == CombatInputType.None && timer < window)
        {
            if (inputBuffer.TryConsume(out var input))
            {
                if (input == CombatInputType.Punch || input == CombatInputType.Kick)
                    chainType = input;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (chainType == CombatInputType.None)
            chainType = CombatInputType.Punch;

        if (afterimageEffect != null)
            afterimageEffect.Activate();

        float animSpeed = _settings != null ? _settings.specialAnimSpeed : 1.5f;
        if (animator != null)
        {
            _originalAnimSpeed = animator.speed;
            animator.speed = animSpeed;
        }

        yield return DoChain(chainType);

        if (animator != null)
            animator.speed = _originalAnimSpeed;

        if (afterimageEffect != null)
            afterimageEffect.Deactivate();

        _isPerforming = false;
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

            if (animator != null)
            {
                animator.SetInteger("ComboStep", comboStep);
                animator.SetInteger("AttackType", (int)chainType);
                animator.SetBool("IsSpecial", true);
                animator.SetTrigger("Attack");
            }

            _hitFinished = false;
            while (!_hitFinished)
                yield return null;
        }

        if (animator != null)
            animator.SetBool("IsSpecial", false);
    }

    private void ConfigureHitbox(CombatInputType chainType, bool isLast)
    {
        if (hitbox == null || _settings == null) return;

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
