using System.Collections;
using UnityEngine;

public enum EntranceType { Walk, Jump }

public class EnemyEntrance : MonoBehaviour
{
    [SerializeField] private EntranceType entranceType = EntranceType.Walk;
    [SerializeField] private Transform target;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private Behaviour[] componentsToDisable;
    
    [SerializeField] private Transform player;
    [SerializeField] private float stopDistance = 1f;
    
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";

    private bool _hasEntered;

    private void Awake()
    {
        SetCombatEnabled(false);
    }

    public void BeginEntrance()
    {
        if (_hasEntered) return;
        _hasEntered = true;
        StartCoroutine(EntranceRoutine());
    }

    private IEnumerator EntranceRoutine()
    {
        Vector3 start = transform.position;
        Vector3 end = target != null ? target.position : start;
        
        Vector3 approachDir = end - start;
        approachDir.y = 0f;
        approachDir = approachDir.sqrMagnitude > 0.0001f ? approachDir.normalized : transform.forward;

        FaceTowards(end);
        
        Rigidbody rb = null;
        bool wasKinematic = false;
        if (TryGetComponent(out rb))
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        if (animator != null && entranceType == EntranceType.Walk)
            animator.SetFloat(speedParam, 1f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float p = Mathf.Clamp01(t);

            Vector3 pos = Vector3.Lerp(start, end, p);
            if (entranceType == EntranceType.Jump)
                pos.y += jumpHeight * Mathf.Sin(p * Mathf.PI);

            transform.position = pos;
            yield return null;
        }
        
        end = KeepClearOfPlayer(end, approachDir);
        transform.position = end;

        if (animator != null && entranceType == EntranceType.Walk)
            animator.SetFloat(speedParam, 0f);

        if (rb != null)
            rb.isKinematic = wasKinematic;

        SetCombatEnabled(true);
    }

    private void FaceTowards(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private void SetCombatEnabled(bool value)
    {
        if (componentsToDisable == null) return;
        foreach (var c in componentsToDisable)
            if (c != null) c.enabled = value;
    }
    
    private Vector3 KeepClearOfPlayer(Vector3 desired, Vector3 approachDir)
    {
        Transform p = ResolvePlayer();
        if (p == null) return desired;

        Vector3 flatDesired = new Vector3(desired.x, 0f, desired.z);
        Vector3 flatPlayer  = new Vector3(p.position.x, 0f, p.position.z);

        if (Vector3.Distance(flatDesired, flatPlayer) >= stopDistance)
            return desired;
        
        Vector3 away = flatDesired - flatPlayer;
        if (away.sqrMagnitude < 0.0001f)
            away = -approachDir;
        if (away.sqrMagnitude < 0.0001f)
            away = Vector3.right;

        away.Normalize();

        Vector3 result = flatPlayer + away * stopDistance;
        result.y = desired.y;
        return result;
    }

    private Transform ResolvePlayer()
    {
        if (player != null) return player;
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        return found != null ? found.transform : null;
    }

    private void OnDrawGizmos()
    {
        if (target == null) return;

        Gizmos.color = entranceType == EntranceType.Jump ? Color.yellow : Color.cyan;

        if (entranceType == EntranceType.Walk)
        {
            Gizmos.DrawLine(transform.position, target.position);
        }
        else
        {
            Vector3 prev = transform.position;
            for (int i = 1; i <= 16; i++)
            {
                float p = i / 16f;
                Vector3 point = Vector3.Lerp(transform.position, target.position, p);
                point.y += jumpHeight * Mathf.Sin(p * Mathf.PI);
                Gizmos.DrawLine(prev, point);
                prev = point;
            }
        }

        Gizmos.DrawWireSphere(target.position, 0.3f);
    }
}