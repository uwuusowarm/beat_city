using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private DashStrike dashStrike;

    private void Awake()
    {
        if (playerCombat == null)
        {
            playerCombat = GetComponentInParent<PlayerCombat>();

            if (playerCombat != null)
            {
                Debug.LogWarning("ref to PlayerCombat is missing in AnimationEventRelay");
            }
        }

        if (dashStrike == null)
        {
            dashStrike = GetComponentInParent<DashStrike>();
        }

        Debug.Log("[Relay] Awake. PlayerCombat found: " + (playerCombat != null) +
                  ", DashStrike found: " + (dashStrike != null));
    }

    public void EnableHitbox()
    {
        Debug.Log("[Relay] EnableHitbox called");

        playerCombat.EnableHitbox();
    }

    public void DisableHitbox()
    {
        Debug.Log("[Relay] DisableHitbox called");

        playerCombat.DisableHitbox();
    }

    public void FinishAttack()
    {
        Debug.Log("[Relay] FinishAttack called");

        playerCombat.FinishAttack();
    }

    public void SpawnSpecialVFX()
    {
        playerCombat.SpawnSpecialVfx();
    }

    public void DashStrikeHit()
    {
        Debug.Log("[Relay] DashStrikeHit called");

        if (dashStrike != null) dashStrike.OnStrikeHit();
    }

    public void DashStrikeSeek()
    {
        Debug.Log("[Relay] DashStrikeSeek called");

        if (dashStrike != null) dashStrike.OnSeekNextTarget();
    }

    public void DashStrikeEnd()
    {
        Debug.Log("[Relay] DashStrikeEnd called");

        if (dashStrike != null) dashStrike.OnDashStrikeEnd();
    }
}