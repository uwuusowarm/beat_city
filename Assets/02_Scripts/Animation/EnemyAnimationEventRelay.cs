using UnityEngine;

public class EnemyAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private EnemyCombat enemyCombat;

    public void EnableAttackHitbox()
    {
        enemyCombat.EnableAttackHitbox();
    }

    public void DisableAttackHitbox()
    {
        enemyCombat.DisableAttackHitbox();
    }


}