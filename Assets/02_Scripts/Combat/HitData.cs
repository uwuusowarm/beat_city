using UnityEngine;

public enum JuggleType
{
    None,
    
    Launcher,
    
    Juggle,
    
    Spike, 
}

public struct HitData
{
    public int Damage;
    public Vector3 KnockbackDirection;
    public float KnockbackForce;       
    public float KnockUpForce;        
    public float HitStunDuration;
    public bool ShouldKnockdown;
    public GameObject Source;
    

    public JuggleType JuggleType;
    
    public bool IsLauncher;
    
    public bool IgnoreJuggleDecay;
    
    public bool SuppressHitAnimation;
}
