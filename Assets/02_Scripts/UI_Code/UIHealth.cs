using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIHealth : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Slider slider;
 
    private void OnEnable()
    {
        if (health == null) return;
 
        health.OnHit += HandleHit;
        health.OnDeath += HandleDeath;
        
        UpdateSlider();
    }
 
    private void OnDisable()
    {
        if (health == null) return;
 
        health.OnHit -= HandleHit;
        health.OnDeath -= HandleDeath;
    }
 
    private void HandleHit(HitData data)
    {
        UpdateSlider();
    }
 
    private void HandleDeath()
    {
        UpdateSlider();
    }
 
    private void UpdateSlider()
    {
        if (slider != null && health != null)
        {
            slider.value = (float)health.Current / health.Max;
        }
    }
    public void Refresh() => UpdateSlider();
}