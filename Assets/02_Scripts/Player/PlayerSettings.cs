using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "ScriptableObjects/PlayerSettings", order = 1)]
public class PlayerSettings : ScriptableObject
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 8f;
    public float gravity = 20f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;

    [Header("Combat Settings")]
    public float attackActiveTime = 0.2f;
    public int punchDamage = 10;
    public float punchBaseKnockback = 2f;
    public float punchFinisherKnockup = 5f;
    public int kickDamage = 15;
    public float kickBaseKnockback = 3f;
    public float kickFinisherKnockback = 8f;
    public int maxComboSteps = 3;
    public float comboResetTime = 0.8f;
    public float jugglingForce = 3f;

    [Header("Grapple Settings")]
    public float grappleActiveTime = 0.2f;
    public int grappleDamage = 15;
    public float throwDistance = 2f;
    public float throwHeight = 1.5f;
    public float throwDuration = 0.4f;
    public float grappleCooldown = 0.5f;
    public float holdOffset = 1.2f;

    [Header("Grapple Projectile Settings")]
    public int projectileDamage = 10;
    public float projectileKnockback = 3f;
    public float projectileKnockUp = 3f;
    public float projectileRadius = 1f;

    [Header("Grapple Impact Settings")]
    public float impactKnockback = 0f;
    public float impactKnockUp = 0f;
}
