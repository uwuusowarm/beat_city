using System.Collections.Generic;
using UnityEngine;

public class BeatEmUpDirector : MonoBehaviour
{
    public static BeatEmUpDirector Instance { get; private set; }

    [SerializeField] private int maxSimultaneousAttackers = 2;
    
    [SerializeField] private int totalFlankSlots = 10; 
    [SerializeField] private float flankRadius = 4.5f; 

    private List<EnemyCombat> _activeAttackers = new List<EnemyCombat>();
    private Dictionary<int, EnemyCombat> _occupiedSlots = new Dictionary<int, EnemyCombat>();
    private Transform _player;

    private void Awake()
    {
        Instance = this;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void Update()
    {
        _activeAttackers.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);
        
        List<int> keysToRemove = new List<int>();
        foreach (var kvp in _occupiedSlots)
        {
            if (kvp.Value == null || !kvp.Value.gameObject.activeInHierarchy)
                keysToRemove.Add(kvp.Key);
        }
        foreach (var key in keysToRemove) _occupiedSlots.Remove(key);
    }

    public bool HasToken(EnemyCombat enemy)
    {
        return _activeAttackers.Contains(enemy);
    }

    public bool RequestAttackToken(EnemyCombat enemy)
    {
        if (_activeAttackers.Contains(enemy)) return true;

        if (_activeAttackers.Count < maxSimultaneousAttackers)
        {
            ReleaseFlankSlot(enemy);
            _activeAttackers.Add(enemy);
            return true;
        }

        return false;
    }

    public void ReleaseToken(EnemyCombat enemy)
    {
        if (_activeAttackers.Contains(enemy))
        {
            _activeAttackers.Remove(enemy);
        }
    }

    public Vector3 GetFlankSlotPosition(EnemyCombat enemy)
    {
        if (_player == null) return enemy.transform.position;

        int assignedSlot = -1;

        foreach (var kvp in _occupiedSlots)
        {
            if (kvp.Value == enemy) { assignedSlot = kvp.Key; break; }
        }

        if (assignedSlot == -1)
        {
            for (int i = 0; i < totalFlankSlots; i++)
            {
                if (!_occupiedSlots.ContainsKey(i))
                {
                    _occupiedSlots[i] = enemy;
                    assignedSlot = i;
                    break;
                }
            }
        }

        if (assignedSlot == -1)
        {
            return _player.position + Vector3.right * (flankRadius + 5f);
        }

        bool isRightSide = (assignedSlot % 2 == 0);
        int row = assignedSlot / 2; 

        float currentDist = flankRadius + (row * 1.5f);
        float dirX = isRightSide ? 1f : -1f;

        float zStagger = (row % 2 == 0) ? 0.3f : -0.3f;

        Vector3 slotOffset = new Vector3(dirX * currentDist, 0f, zStagger);
        return _player.position + slotOffset;
    }

    public void ReleaseFlankSlot(EnemyCombat enemy)
    {
        int keyToRemove = -1;
        foreach (var kvp in _occupiedSlots)
        {
            if (kvp.Value == enemy) { keyToRemove = kvp.Key; break; }
        }
        if (keyToRemove != -1) _occupiedSlots.Remove(keyToRemove);
    }

    private void OnDrawGizmos()
    {
        if (_player == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < totalFlankSlots; i++)
        {
            bool isRightSide = (i % 2 == 0);
            int row = i / 2;
            float currentDist = flankRadius + (row * 1.5f);
            float dirX = isRightSide ? 1f : -1f;
            float zStagger = (row % 2 == 0) ? 0.3f : -0.3f;

            Vector3 pos = _player.position + new Vector3(dirX * currentDist, 0f, zStagger);
            Gizmos.DrawWireSphere(pos, 0.3f);
        }
    }
}