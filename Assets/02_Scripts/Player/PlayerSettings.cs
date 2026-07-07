using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "ScriptableObjects/PlayerSettings", order = 1)]
public class PlayerSettings : ScriptableObject
{
    [Header("Movement Settings")]
    [Tooltip("Normal walking speed")]
    public float walkSpeed = 5f;
    
    [Tooltip("Jump force applied when jumping")]
    public float jumpForce = 8f;
    
    [Tooltip("Gravity applied to player")]
    public float gravity = 20f;

    [Header("Punch Combat")]
    [Tooltip("Damage dealt by punch attacks")]
    public int punchDamage = 10;
    
    [Tooltip("Knockback force for normal punches")]
    public float punchBaseKnockback = 2f;
    
    [Tooltip("Upward force for punch finisher (launcher)")]
    public float punchFinisherKnockup = 5f;

    [Header("Kick Combat")]
    [Tooltip("Damage dealt by kick attacks")]
    public int kickDamage = 15;
    
    [Tooltip("Knockback force for normal kicks")]
    public float kickBaseKnockback = 3f;
    
    [Tooltip("Knockback force for kick finisher")]
    public float kickFinisherKnockback = 8f;

    [Header("Special Combat")]
    [Tooltip("Damage dealt by special attack")]
    public int specialDamage = 5;

    [Tooltip("Knockup force for special attack")]
    public float specialKnockup = 5f;

    [Tooltip("Amount needed to use special attack")]
    public int specialCost = 30;


    [Header("Combo System")]
    [Tooltip("Maximum number of hits in a combo")]
    public int maxComboSteps = 3;
    
    [Tooltip("Time before combo resets if no input")]
    public float comboResetTime = 0.8f;
    
    [Tooltip("Upward force applied during juggle hits (non-finisher)")]
    public float jugglingForce = 3f;

    [Header("Grapple Settings")]
    [Tooltip("How long the grapple hitbox is active")]
    public float grappleActiveTime = 0.2f;
    
    [Tooltip("Damage dealt when throwing an enemy")]
    public int grappleDamage = 15;
    
    [Tooltip("How far the enemy is thrown")]
    public float throwDistance = 2f;
    
    [Tooltip("Peak height of throw arc")]
    public float throwHeight = 1.5f;
    
    [Tooltip("Duration of throw animation")]
    public float throwDuration = 0.4f;
    
    [Tooltip("Cooldown before next grapple")]
    public float grappleCooldown = 0.5f;
    
    [Tooltip("Distance enemy is held in front of player")]
    public float holdOffset = 1.2f;

    [Tooltip("Extra distance added to backward throw to compensate for hold offset")]
    public float backwardThrowOffset = 2.4f;

    [Tooltip("At what % of the Headbutt animation the enemy starts flying (0-1)")]
    [Range(0f, 1f)]
    public float headbuttLaunchPoint = 0.8f;

    [Tooltip("At what % of the Throw animation the enemy starts flying (0-1)")]
    [Range(0f, 1f)]
    public float throwLaunchPoint = 0f;

    [Header("Throw Projectile (Enemy hits other enemies)")]
    [Tooltip("Damage dealt when thrown enemy hits another enemy")]
    public int projectileDamage = 10;
    
    [Tooltip("Knockback when thrown enemy hits another enemy")]
    public float projectileKnockback = 3f;
    
    [Tooltip("Upward force when thrown enemy hits another enemy")]
    public float projectileKnockUp = 3f;
    
    [Tooltip("Collision radius for thrown enemy")]
    public float projectileRadius = 1f;

    [Header("Throw Impact (On thrown enemy)")]
    [Tooltip("Knockback applied to thrown enemy on landing")]
    public float impactKnockback = 0f;
    
    [Tooltip("Upward force applied to thrown enemy on landing")]
    public float impactKnockUp = 0f;
}
