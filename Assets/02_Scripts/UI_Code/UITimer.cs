using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class UITimer : MonoBehaviour
{
    [SerializeField] private float startTime = 99f;
    [SerializeField] private bool countDown = true;
    [SerializeField] private TextMeshProUGUI timerText;
    public UnityEvent onTimerExpired;

    private float currentTime;
    private bool isRunning;

    private void Start()
    {
        ResetTimer();
        StartTimer();
    }

    private void Update()
    {
        if (!isRunning) return;

        if (countDown)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                isRunning = false;
                onTimerExpired?.Invoke();
            }
        }
        else
        {
            currentTime += Time.deltaTime;
        }
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        int displaySeconds = Mathf.CeilToInt(currentTime);
        timerText.text = displaySeconds.ToString();
    }

    public void StartTimer() => isRunning = true;

    public void StopTimer() => isRunning = false;

    public void ResetTimer()
    {
        currentTime = startTime;
        isRunning = false;
        UpdateDisplay();
    }

    public void AddTime(float seconds)
    {
        currentTime += seconds;
        UpdateDisplay();
    }
    public float GetCurrentTime() => currentTime;
}