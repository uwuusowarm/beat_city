using TMPro;
using UnityEngine;

public class KeybindMenuUI : MonoBehaviour
{
    [SerializeField] private KeybindManager keybindManager;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI punchLabel;
    [SerializeField] private TextMeshProUGUI kickLabel;
    [SerializeField] private TextMeshProUGUI specialLabel;

    [Header("Text")]
    [SerializeField] private string listeningText = "Press a key";
    [SerializeField] private string unboundText = "None";

    private bool _wasListening;

    private void OnEnable()
    {
        if (keybindManager == null) return;

        keybindManager.BindingsChanged += RefreshLabels;
        RefreshLabels();
    }

    private void OnDisable()
    {
        if (keybindManager == null) return;

        keybindManager.BindingsChanged -= RefreshLabels;
        keybindManager.CancelRebind();
        _wasListening = false;
    }

    private void Update()
    {
        if (keybindManager == null) return;

        if (_wasListening && !keybindManager.IsListening)
        {
            _wasListening = false;
            RefreshLabels();
        }
    }

    public void RebindPunch()
    {
        BeginRebind(KeybindManager.RebindTarget.Punch, punchLabel);
    }

    public void RebindKick()
    {
        BeginRebind(KeybindManager.RebindTarget.Kick, kickLabel);
    }

    public void RebindSpecial()
    {
        BeginRebind(KeybindManager.RebindTarget.Special, specialLabel);
    }

    public void CancelRebind()
    {
        if (keybindManager == null) return;

        keybindManager.CancelRebind();
        _wasListening = false;
        RefreshLabels();
    }

    public void ResetToDefaults()
    {
        if (keybindManager == null) return;

        keybindManager.CancelRebind();
        _wasListening = false;
        keybindManager.ResetToDefaults();
    }

    private void BeginRebind(KeybindManager.RebindTarget target, TextMeshProUGUI label)
    {
        RefreshLabels();
        keybindManager.StartRebind(target);
        _wasListening = true;

        if (label != null) label.text = listeningText;
    }

    private void RefreshLabels()
    {
        SetLabel(punchLabel, KeybindManager.RebindTarget.Punch);
        SetLabel(kickLabel, KeybindManager.RebindTarget.Kick);
        SetLabel(specialLabel, KeybindManager.RebindTarget.Special);
    }

    private void SetLabel(TextMeshProUGUI label, KeybindManager.RebindTarget target)
    {
        if (label == null) return;

        string displayName = keybindManager.GetBindingDisplayName(target);
        label.text = string.IsNullOrEmpty(displayName) ? unboundText : displayName;
    }
}
