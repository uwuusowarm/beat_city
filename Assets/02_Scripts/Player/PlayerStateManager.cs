using System;
using UnityEngine;

public enum PlayerState
{
    Idle,
    Attacking,
    Grappling,
    Holding,
    Dashing,
    Stunned,
    SpecialAttacking
}

public class PlayerStateManager : MonoBehaviour
{
    public static PlayerStateManager Instance { get; private set; }

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    
    public event Action<PlayerState> OnStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool CanPerformAction()
    {
        return CurrentState == PlayerState.Idle;
    }

    public void SetState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        Debug.Log($"[PlayerStateManager] State changed to: {newState}");
    }

    public void ResetToIdle()
    {
        SetState(PlayerState.Idle);
    }
}
