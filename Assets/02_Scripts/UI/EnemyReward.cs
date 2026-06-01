using UnityEngine;
 
public class EnemyReward : MonoBehaviour
{
    [SerializeField] private int reward;
    [SerializeField] private Money playerMoney;
 
    private Health health;
 
    private void Awake()
    {
        health = GetComponent<Health>();
    }
 
    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }
 
    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }
 
    private void HandleDeath()
    {
        if (playerMoney != null)
        {
            playerMoney.Add(reward);
            Debug.Log($"[Money] {gameObject.name} killed, awarded ${reward}");
        }
    }
}
