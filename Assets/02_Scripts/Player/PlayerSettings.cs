﻿using UnityEngine;

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

    [Tooltip("Offset of the grab box, local to the anchor transform")]
    public Vector3 grabBoxOffset = new Vector3(0f, 0f, 0.8f);

    [Tooltip("Size of the grab box")]
    public Vector3 grabBoxSize = new Vector3(1f, 1f, 0.7f);

    [Tooltip("Only grab if the player is actively moving toward the enemy. Off = old instant-on-overlap behaviour.")]
    public bool requireGrabIntent = true;

    [Tooltip("How long the player must already have been moving toward the enemy")]
    [Range(0f, 0.5f)]
    public float grabIntentTime = 0.12f;

    [Tooltip("Dot product threshold: how precisely the movement must point at the enemy. Max deviation to each side: 1 = 0°, 0.87 = 30°, 0.5 = 60°, 0 = 90°. Higher = stricter.")]
    [Range(0f, 1f)]
    public float grabIntentDot = 0.5f;

    [Tooltip("Distance enemy is held in front of player")]
    public float holdOffset = 1.2f;

    [Tooltip("Local position offset applied while holding a grabbed enemy")]
    public Vector3 grappleHoldOffset = Vector3.zero;

    [Tooltip("Extra flight distance added to Throw Distance for backward throws only. Pure reach - the carry already brings the enemy to the front, nothing here compensates for that.")]
    public float backwardThrowOffset = 2.4f;

    [Tooltip("At what % of the Headbutt animation the enemy starts flying (0-1)")]
    [Range(0f, 1f)]
    public float headbuttLaunchPoint = 0.8f;

    [Tooltip("At what % of the Throw animation the enemy starts flying (0-1). Keep below 0.75 - above that the Throw state has already blended into Idle.")]
    [Range(0f, 1f)]
    public float throwLaunchPoint = 0f;

    [Tooltip("Backward throw only: carry the grabbed enemy around the player while the Throw clip turns him 180 degrees. Off = old behaviour (enemy frozen in place, compensated by Backward Throw Offset).")]
    public bool throwCarryEnabled = true;

    [Tooltip("Backward throw only: at what % of the Throw animation the enemy has finished swinging to the new front side. Clamped to Throw Launch Point; the enemy waits there until launch.")]
    [Range(0f, 1f)]
    public float throwCarryTurnEnd = 0.45f;

    [Tooltip("Backward throw only: how the swing is spread over its time window. Below 1 front-loads it so the enemy whips around immediately and settles - match this to a clip that turns right at the start. 1 = even ease in and out. Above 1 holds him back and moves him late.")]
    [Range(0.25f, 4f)]
    public float throwCarryEase = 1f;

    [Tooltip("Backward throw only: extra height added while the enemy is swung around. 0 = flat orbit at normal hold height.")]
    [Range(-1f, 1.5f)]
    public float throwCarryLift = 0f;

    [Tooltip("Backward throw only: scales the radius of the whole half circle the enemy is dragged around. 1 = swung around at Hold Offset, 2 = twice as wide, 0.5 = half. Eases in and out at the very ends so grab and release stay at normal hold distance.")]
    [Range(0.2f, 3f)]
    public float throwCarryRadiusScale = 1f;

    [Tooltip("Backward throw only: extra radius on top of Radius Scale, peaking at the middle of the swing only. Positive arcs wide, negative drags the enemy closer past the body, 0 = even circle.")]
    [Range(-1.5f, 2f)]
    public float throwCarryBulge = 0f;

    [Tooltip("Backward throw only: swing the enemy past the player's own right side. Flip if it does not match the side the Throw clip turns through.")]
    public bool throwCarrySweepRight = true;

    [Header("Throw Projectile (Enemy hits other enemies)")]
    [Tooltip("Damage dealt when thrown enemy hits another enemy")]
    public int projectileDamage = 10;
    
    [Tooltip("Knockback when thrown enemy hits another enemy")]
    public float projectileKnockback = 3f;
    
    [Tooltip("Upward force when thrown enemy hits another enemy")]
    public float projectileKnockUp = 3f;
    
    [Tooltip("Collision radius for thrown enemy")]
    public float projectileRadius = 1f;

    [Header("Special Chain Move")]
    [Tooltip("Meter cost to activate a special move")]
    public float specialMeterCost = 50f;

    [Tooltip("Number of punches in the special punch chain")]
    public int specialPunchCount = 3;

    [Tooltip("Number of kicks in the special kick chain")]
    public int specialKickCount = 3;

    [Tooltip("Damage per punch in the special chain")]
    public int specialPunchDamage = 15;

    [Tooltip("Damage per kick in the special chain")]
    public int specialKickDamage = 20;

    [Tooltip("Knockup force on the final punch of the special chain")]
    public float specialFinisherKnockup = 8f;

    [Tooltip("Knockback force on the final kick of the special chain")]
    public float specialFinisherKnockback = 10f;

    [Tooltip("Time window to choose punch or kick after pressing special")]
    public float specialInputWindow = 1f;

    [Tooltip("Animation speed multiplier during special chain (1 = normal, 2 = double speed)")]
    public float specialAnimSpeed = 1.5f;

    [Header("Throw Impact (On thrown enemy)")]
    [Tooltip("Knockback applied to thrown enemy on landing")]
    public float impactKnockback = 0f;
    
    [Tooltip("Upward force applied to thrown enemy on landing")]
    public float impactKnockUp = 0f;

    [Header("Hit Stop")]
    [Tooltip("Freeze-frame duration when player attacks damage enemies")]
    [Range(0f, 0.3f)]
    public float combatHitStop = 0.05f;

    [Tooltip("Freeze-frame duration when thrown enemy collides with another enemy")]
    [Range(0f, 0.3f)]
    public float grappleProjectileHitStop = 0.05f;

    [Tooltip("Freeze-frame duration when thrown enemy takes the final impact hit")]
    [Range(0f, 0.3f)]
    public float grappleImpactHitStop = 0.08f;

    [Header("Afterimage")]
    [Tooltip("Master switch for the afterimage ghost trail. Off suppresses it everywhere, regardless of the per-attack toggles below")]
    public bool afterimageEnabled = true;

    [Tooltip("Afterimage trail during the special chain (punch/kick chain)")]
    public bool specialChainAfterimage = true;

    [Tooltip("Afterimage trail during the dash strike")]
    public bool dashStrikeAfterimage = true;

    [Header("Speed Lines")]
    [Tooltip("Master switch for the speed line streaks. Off suppresses them everywhere, regardless of the per-attack toggles below")]
    public bool speedLinesEnabled = true;

    [Tooltip("Speed lines during the dash strike")]
    public bool dashStrikeSpeedLines = true;

    [Tooltip("Minimum movement speed in units per second before lines are emitted. Keeps them off during the frozen strike poses where the player stands still at the enemy")]
    public float speedLinesMinSpeed = 8f;

    [Tooltip("Multiplier on the rate over distance authored in the particle prefab. 1 = the prefab value, higher = more lines per unit travelled")]
    public float speedLinesDensity = 1f;

    [Tooltip("Multiplier on the stretched billboard length scale authored in the particle prefab. 1 = the prefab value, higher = longer streaks")]
    public float speedLinesLengthScale = 1f;

    [Tooltip("Start color written to the particle system on activation. The prefab's Color over Lifetime fade is applied on top and stays intact")]
    public Color speedLinesColor = Color.white;

    [Header("Dash Strike Special - Hits")]
    [Tooltip("Number of strikes. Must match the number of DashStrikeHit events in the dash clip")]
    public int dashStrikeHitCount = 3;

    [Tooltip("Damage of every strike except the last one")]
    public int dashStrikeDamage = 20;

    [Tooltip("Damage of the final strike")]
    public int dashStrikeFinalDamage = 30;

    [Header("Dash Strike Special - Targeting")]
    [Tooltip("Radius around the player in which the next enemy is searched")]
    public float dashStrikeSearchRadius = 15f;

    [Tooltip("Distance in front of the target where the player comes to a stop")]
    public float dashStrikeApproachDistance = 1.2f;

    [Tooltip("Pick enemies that were not hit yet in this attack first. Falls back to already hit ones so all strikes land even with a single enemy around")]
    public bool dashStrikePreferNewTargets = true;

    [Tooltip("Abort the special and refund the meter when no enemy is within the search radius")]
    public bool dashStrikeRequireTarget = true;

    [Header("Dash Strike Special - Dash")]
    [Tooltip("Travel speed toward the target in units per second")]
    public float dashStrikeSpeed = 40f;

    [Tooltip("Safety timeout per leg. The animation resumes even if the target was never reached")]
    public float dashStrikeMaxTravelTime = 0.5f;

    [Tooltip("Travel on unscaled time so the dash stays snappy during the slow motion of the previous hit")]
    public bool dashStrikeUnscaledTravel = true;

    [Tooltip("Freeze the animation while travelling to the next target and resume it on arrival. Guarantees every strike pose happens at an enemy")]
    public bool dashStrikeHoldAnimUntilArrival = true;

    [Tooltip("Snap onto the target if the strike event fires before the dash arrived")]
    public bool dashStrikeSnapOnHit = true;

    [Header("Dash Strike Special - Launch")]
    [Tooltip("Upward force per strike. Capped by EnemySettings maxJuggleHeight / maxJugglingVelocity")]
    public float dashStrikeKnockup = 6f;

    [Tooltip("Upward force of the final strike")]
    public float dashStrikeFinalKnockup = 10f;

    [Tooltip("Horizontal knockback per strike")]
    public float dashStrikeKnockback = 1f;

    [Tooltip("Horizontal knockback of the final strike")]
    public float dashStrikeFinalKnockback = 4f;

    [Tooltip("Knock the target down with the final strike instead of leaving it juggled")]
    public bool dashStrikeFinalKnockdown = true;

    [Header("Dash Strike Special - Hitbox")]
    [Tooltip("Size of the dash strike hitbox, written before every strike. The hitbox transform does not rotate with the character model, so keep it wide enough to cover Approach Distance in every direction")]
    public Vector3 dashStrikeHitboxSize = new Vector3(3f, 3f, 3f);

    [Tooltip("Local offset of the dash strike hitbox, written before every strike. Keep it centered - a forward offset would point at world +Z instead of at the target")]
    public Vector3 dashStrikeHitboxOffset = Vector3.zero;

    [Header("Dash Strike Special - Time Scale Dip")]
    [Tooltip("Freeze-frame duration per strike")]
    [Range(0f, 0.25f)]
    public float dashStrikeHitFreeze = 0.05f;

    [Tooltip("Slow motion duration after the freeze frame")]
    [Range(0f, 0.6f)]
    public float dashStrikeSlowMoDuration = 0.12f;

    [Tooltip("Time scale during the slow motion. 1 = no slow motion")]
    [Range(0.01f, 1f)]
    public float dashStrikeSlowMoTimeScale = 0.35f;

    [Tooltip("Freeze-frame duration of the final strike")]
    [Range(0f, 0.25f)]
    public float dashStrikeFinalHitFreeze = 0.12f;

    [Tooltip("Slow motion duration of the final strike")]
    [Range(0f, 0.6f)]
    public float dashStrikeFinalSlowMoDuration = 0.3f;

    [Tooltip("Time scale during the slow motion of the final strike")]
    [Range(0.01f, 1f)]
    public float dashStrikeFinalSlowMoTimeScale = 0.2f;

    [Header("Dash Strike Special - Flow")]
    [Tooltip("Animation speed multiplier while the dash strike plays")]
    public float dashStrikeAnimSpeed = 1f;

    [Tooltip("Wait time after the last strike before control is handed back")]
    public float dashStrikeRecovery = 0.15f;

    [Tooltip("Watchdog. The special is force-ended after this many seconds")]
    public float dashStrikeMaxDuration = 3f;
}
