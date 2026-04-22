using System;
using UnityEngine;
 
public class Money : MonoBehaviour
{
    public int Current { get; private set; }
 
    public event Action<int> OnMoneyChanged;
 
    public void Add(int amount)
    {
        Current += amount;
        OnMoneyChanged?.Invoke(Current);
    }
 
    public bool TrySpend(int amount)
    {
        if (Current < amount) return false;
 
        Current -= amount;
        OnMoneyChanged?.Invoke(Current);
        return true;
    }
}