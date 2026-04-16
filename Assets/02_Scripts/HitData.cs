using UnityEngine;

public struct HitData
{
    public int Damage;
    public Vector3 KnockbackDirection; // normalisiert, Y=0
    public float KnockbackForce;       // horizontaler Rückstoß
    public float KnockUpForce;         // vertikaler Aufstoß (0 = normaler Treffer)
    public float HitStunDuration;
    public GameObject Source;
}
