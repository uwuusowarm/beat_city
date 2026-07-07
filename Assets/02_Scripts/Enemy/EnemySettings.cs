using UnityEngine;

[CreateAssetMenu(fileName = "EnemySettings", menuName = "ScriptableObjects/EnemySettings")]
public class EnemySettings : ScriptableObject
{
    [Header("Health")]
    [Tooltip("Maximum health of the enemy")]
    public int maxHealth = 100;

    [Header("Movement")]
    [Tooltip("Base movement speed")]
    public float moveSpeed = 3f;
    
    [Tooltip("Distance at which enemy stops approaching player")]
    public float stopDistance = 1.5f;
    
    [Tooltip("Distance at which enemy can attack")]
    public float attackDistance = 1.5f;

    [Header("AI Tactics")]
    [Tooltip("Minimum time before changing tactic")]
    public float minRepositionTime = 1.0f;
    
    [Tooltip("Maximum time before changing tactic")]
    public float maxRepositionTime = 3.0f;

    [Header("Combat / Hitstun")]
    [Tooltip("Hitstun duration for combo hits")]
    public float comboHitStun = 0.5f;
    
    [Tooltip("Hitstun duration for kick attacks")]
    public float kickStunDuration = 1.5f;

    [Header("Juggle Parameters")]
    [Tooltip("Maximum vertical velocity during juggle")]
    public float maxJugglingVelocity = 15f;
    
    [Tooltip("Maximum height above ground the enemy can be juggled to")]
    public float maxJuggleHeight = 4f;
    
    [Tooltip("Minimum KnockUpForce required to launch a grounded enemy")]
    public float launchThreshold = 3.5f;
    
    [Tooltip("Juggle force reduction per consecutive hit (0.15 = 15% less per hit)")]
    public float juggleFalloffPerHit = 0.15f;
    
    [Tooltip("Minimum juggle effectiveness (0.30 = 30% of original force)")]
    public float juggleMinScale = 0.30f;
    
    [Tooltip("Maximum number of juggle hits before forced knockdown")]
    public int maxJuggleCount = 10;

    [Tooltip("How long the enemy hovers in the air after a juggle hit (seconds)")]
    public float juggleHoverDuration = 0.5f;

    [Header("Gravity")]
    public float baseGravity = 20f;
    
    [Tooltip("If true, gravity increases slightly during fall for better game feel")]
    public bool useGravityScaling = true;
    
    [Tooltip("Gravity multiplier when falling (1.5 = 50% faster fall)")]
    public float fallGravityMultiplier = 1.5f;

    [Header("Ground Detection")]
    [Tooltip("Raycast distance for ground check")]
    public float groundCheckDistance = 0.2f;
    
    [Tooltip("Layers considered as ground")]
    public LayerMask groundLayer;

    [Header("Recovery")]
    [Tooltip("Time enemy stays on ground after knockdown")]
    public float knockdownDuration = 1.0f;
    
    [Tooltip("Duration of stand-up animation")]
    public float standUpDuration = 1.0f;

    [Header("Visuals / Animation")]
    [Tooltip("How fast the fall animation wobbles when paused")]
    public float fallWobbleSpeed = 4f;
    
    [Tooltip("How much the fall animation wobbles when paused")]
    public float fallWobbleIntensity = 0.02f;
    
    [Tooltip("At what normalized time (0.0 to 1.0) the fall animation should pause")]
    public float fallAnimationHoldPoint = 0.2f;

    [Header("Death / Despawn")]
    [Tooltip("Time before the enemy object is destroyed after death")]
    public float despawnDelay = 3.0f;

    [Header("Attack Commit")]
    [Tooltip("How long the enemy telegraphs (winds up) before an attack actually lands, in seconds")]
    public float telegraphDuration = 0.35f;

    [Tooltip("If true, the enemy stops repositioning while telegraphing (winding up) an attack, giving the player a chance to escape the attack range")]
    public bool freezeDuringTelegraph = true;

    [Tooltip("Movement speed multiplier applied while telegraphing an attack (0 = fully frozen, 1 = normal speed). Only used if freezeDuringTelegraph is enabled")]
    [Range(0f, 1f)]
    public float telegraphMoveSpeedMultiplier = 0f;

}
