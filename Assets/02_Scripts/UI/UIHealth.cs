using System;
using UnityEngine;
using UnityEngine.UI;

public class UIHealth : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider trailSlider;
    [SerializeField] private float trailDelay = 0.8f;
    [SerializeField] private float trailSpeed = 0.4f;
    [SerializeField] private bool hideIfNoTarget = false;
    [SerializeField] private GameObject visualRoot;
 
    private float trailTimer;
 
    public void SetTarget(Health newHealth)
    {
        if (health == newHealth)
        {
            if (health != null && hideIfNoTarget && visualRoot != null && !visualRoot.activeSelf)
            {
                visualRoot.SetActive(true);
            }
            UpdateSlider();
            return;
        }

        Unsubscribe();
        health = newHealth;
        Subscribe();
        
        if (health != null)
        {
            UpdateSlider();
            if (trailSlider != null)
                trailSlider.value = healthSlider.value;
            
            if (hideIfNoTarget && visualRoot != null)
                visualRoot.SetActive(true);
        }
    }

    private void Subscribe()
    {
        if (health == null) return;
        health.OnHit += HandleHit;
        health.OnDeath += HandleDeath;
    }

    private void Unsubscribe()
    {
        if (health == null) return;
        health.OnHit -= HandleHit;
        health.OnDeath -= HandleDeath;
    }

    private void OnEnable()
    {
        Subscribe();

        if (health != null && trailSlider != null)
            trailSlider.value = healthSlider.value;
    }

    private void Start()
    {
        UpdateSlider();
        if (hideIfNoTarget && health == null && visualRoot != null)
        {
            visualRoot.SetActive(false);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
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
        
        if (hideIfNoTarget && visualRoot != null)
        {
            visualRoot.SetActive(false);
            Unsubscribe();
            health = null;
        }
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