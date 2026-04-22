using UnityEngine;
using TMPro;

public class UIPoints : MonoBehaviour
{
    [SerializeField] private Points points;
    [SerializeField] private TextMeshProUGUI pointsText;

    private void OnEnable() //post awake from hitcounter
    {
        if (points == null) return;

        points.OnPointsChanged += HandlePointsChanged;
        UpdatePoints();
    }

    private void OnDisable()
    {
        if (points == null) return;

        points.OnPointsChanged -= HandlePointsChanged;
    }

    private void HandlePointsChanged(int newTotal)
    {
        UpdatePoints();
    }

    private void UpdatePoints()
    {
        if (pointsText != null && points != null)
        {
            pointsText.text = $"Points: {points.Current:D7}";
        }
    }

    public void RefreshPoints() => UpdatePoints();
}