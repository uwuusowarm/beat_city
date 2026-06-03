using UnityEngine;

public struct HitData
{
    public int Damage;
    public Vector3 KnockbackDirection;
    public float KnockbackForce;       
    public float KnockUpForce;        
    public float HitStunDuration;
    public bool ShouldKnockdown;
    public GameObject Source;
}
