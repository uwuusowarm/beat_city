using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIMeter : MonoBehaviour
{
    [SerializeField] private Meter meter;
    [SerializeField] private Slider meterSlider;
    [SerializeField] private TextMeshProUGUI meterText;
    [SerializeField] private Color fullColor;
    [SerializeField] private Color chargingColor;
    [SerializeField] Image meterFill;

    private void Awake()
    {
        //meterFill = meterSlider.fillRect.GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (meter == null) return;

        meter.OnMeterChanged += HandleMeterChanged;
        UpdateMeter();
    }

    private void OnDisable()
    {
        if (meter == null) return;

        meter.OnMeterChanged -= HandleMeterChanged;
    }

    private void HandleMeterChanged(int newTotal)
    {
        UpdateMeter();
    }

    private void UpdateMeter()
    {
        if (meterText != null && meter != null)
        {
            meterText.text = $"{meter.Current:D3}%";
        }
        if (meterSlider != null && meter != null)
        {
            meterSlider.value = (float)meter.Current / 200;
        }
        meterFill.color = meterSlider.value >= 0.5 ? fullColor : chargingColor;
    }

    public void RefreshMeter() => UpdateMeter();
}