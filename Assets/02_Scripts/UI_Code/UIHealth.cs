using UnityEngine;
using UnityEngine.UI;

public class UIHealth : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider trailSlider;
    [SerializeField] private float trailDelay = 0.8f;
    [SerializeField] private float trailSpeed = 0.4f;
 
    private float trailTimer;
 
    private void OnEnable()
    {
        if (health == null) return;
 
        health.OnHit += HandleHit;
        health.OnDeath += HandleDeath;
        UpdateSlider();

        if (trailSlider != null)
            trailSlider.value = healthSlider.value;
    }
 
    private void OnDisable()
    {
        if (health == null) return;
 
        health.OnHit -= HandleHit;
        health.OnDeath -= HandleDeath;
    }
 
    private void Update()
    {
        if (trailSlider == null) return;

        if (trailTimer > 0f)
        {
            trailTimer -= Time.deltaTime;
            return;
        }

        if (trailSlider.value > healthSlider.value)
        {
            trailSlider.value -= trailSpeed * Time.deltaTime;

            if (trailSlider.value <= healthSlider.value)
                trailSlider.value = healthSlider.value;
        }
    }
 
    private void HandleHit(HitData data)
    {
        UpdateSlider();

        trailTimer = trailDelay;
    }
 
    private void HandleDeath()
    {
        UpdateSlider();
    }
 
    private void UpdateSlider()
    {
        if (healthSlider != null && health != null)
        {
            healthSlider.value = (float)health.Current / health.Max;
        }
    }
    
    public void RefreshHealth() => UpdateSlider();
}