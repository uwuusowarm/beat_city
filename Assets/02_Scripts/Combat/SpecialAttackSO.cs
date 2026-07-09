using UnityEngine;

[CreateAssetMenu(fileName = "New Special Attack", menuName = "ScriptableObjects/SpecialAttackTool")]
public class SpecialAttackSO : ScriptableObject
{
    [Header("General")]
    public string attackName = "New Attack";
    public string animatorTrigger = "Special1"; 
    public float totalDuration = 1.0f;          

    [Header("Hits & Combo Settings")]
    public int hitCount = 1;
    public float timeBetweenHits = 0.1f;
    public int damagePerHit = 10;

    [Header("The Last Hit (Finisher)")]
    public float finalKnockback = 10f;
    public float finalKnockup = 0f;
    public bool finalHitShouldKnockdown = true;

    [Header("Hitbox Form & Size")]
    public Vector3 hitboxSize = new Vector3(1.5f, 1.5f, 1.5f);
    public Vector3 hitboxOffset = new Vector3(0f, 1f, 1f);

    [Header("Movement (Dash)")]
    public bool moveForward = false;
    public float dashSpeed = 10f;

    [Header("Projectile")]
    public bool isProjectile = false;
    public GameObject projectilePrefab; 

    [Header("Shop & System")]
    public string id = "new_special";
    public int shopCost = 500;
    public bool unlockedByDefault = false;

    [Header("Custom Animation Sequence")]
    public bool isComboSequence = false;
    public string[] animationSequence;
    public float animationPlaybackSpeed = 1.0f;
}