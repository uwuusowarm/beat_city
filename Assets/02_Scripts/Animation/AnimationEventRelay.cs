using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;

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

        Debug.Log("[Relay] Awake. PlayerCombat found: " + (playerCombat != null));
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
}