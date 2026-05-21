using System.Collections.Generic;
using UnityEngine;

public class BeatEmUpDirector : MonoBehaviour
{
    public static BeatEmUpDirector Instance { get; private set; }

    [SerializeField] private int maxSimultaneousAttackers = 2;

    private List<EnemyCombat> _activeAttackers = new List<EnemyCombat>();

    private void Awake()
    {
        Instance = this;
    }

    public bool RequestAttackToken(EnemyCombat enemy)
    {
        if (_activeAttackers.Contains(enemy)) return true;

        _activeAttackers.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);

        if (_activeAttackers.Count < maxSimultaneousAttackers)
        {
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
}