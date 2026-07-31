using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeybindManager : MonoBehaviour
{
    public enum RebindTarget
    {
        Punch,
        Kick,
        Special
    }

    [SerializeField] private InputActionReference punchAction;
    [SerializeField] private InputActionReference kickAction;
    [SerializeField] private InputActionReference specialAction;

    public event Action BindingsChanged;

    private bool _isListening;
    private RebindTarget _listeningTarget;
    private bool _skipFrame;

    public bool IsListening => _isListening;

    private void Start()
    {
        if (punchAction == null) return;

        if (!string.IsNullOrEmpty(ControlSettings.BindingsJson))
            GetAsset().LoadBindingOverridesFromJson(ControlSettings.BindingsJson);
    }

    private void Update()
    {
        if (!_isListening) return;

        if (_skipFrame)
        {
            _skipFrame = false;
            return;
        }

        string path = DetectPressedPath();
        if (path == null) return;
        if (IsPathUsedByOtherAction(path)) return;

        ApplyBinding(GetAction(_listeningTarget), path);
        _isListening = false;
        SaveBindings();
    }

    public void StartRebind(RebindTarget target)
    {
        if (punchAction == null || kickAction == null || specialAction == null) return;

        _listeningTarget = target;
        _isListening = true;
        _skipFrame = true;
    }

    public void CancelRebind()
    {
        _isListening = false;
    }

    public void ResetToDefaults()
    {
        if (punchAction == null) return;

        GetAsset().RemoveAllBindingOverrides();
        ControlSettings.BindingsJson = "";
        if (GameDataManager.Instance != null) GameDataManager.Instance.Save();
        BindingsChanged?.Invoke();
    }

    public string GetBindingDisplayName(RebindTarget target)
    {
        if (punchAction == null || kickAction == null || specialAction == null) return "";

        string path = GetCurrentPath(GetAction(target));
        if (string.IsNullOrEmpty(path)) return "";
        return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    private void SaveBindings()
    {
        ControlSettings.BindingsJson = GetAsset().SaveBindingOverridesAsJson();
        if (GameDataManager.Instance != null) GameDataManager.Instance.Save();
        BindingsChanged?.Invoke();
    }

    private string DetectPressedPath()
    {
        if (Keyboard.current != null)
        {
            for (Key key = Key.A; key <= Key.Z; key++) //only a to z to prevent weird stuff happening
            {
                if (Keyboard.current[key].wasPressedThisFrame)
                    return "<Keyboard>/" + Keyboard.current[key].name;
            }
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) return "<Mouse>/leftButton";
            if (Mouse.current.rightButton.wasPressedThisFrame) return "<Mouse>/rightButton";
        }

        return null;
    }

    private bool IsPathUsedByOtherAction(string path)
    {
        foreach (RebindTarget target in Enum.GetValues(typeof(RebindTarget)))
        {
            if (target == _listeningTarget) continue;
            if (GetCurrentPath(GetAction(target)) == path) return true;
        }
        return false;
    }

    private void ApplyBinding(InputAction action, string path)
    {
        bool first = true;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (!IsKeyboardOrMouse(action.bindings[i].path)) continue;
            action.ApplyBindingOverride(i, first ? path : "");
            first = false;
        }
    }

    private string GetCurrentPath(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (!IsKeyboardOrMouse(action.bindings[i].path)) continue;
            return action.bindings[i].effectivePath;
        }
        return "";
    }

    private static bool IsKeyboardOrMouse(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        return path.StartsWith("<Keyboard>") || path.StartsWith("<Mouse>");
    }

    private InputAction GetAction(RebindTarget target)
    {
        switch (target)
        {
            case RebindTarget.Kick: return kickAction.action;
            case RebindTarget.Special: return specialAction.action;
            default: return punchAction.action;
        }
    }

    private InputActionAsset GetAsset()
    {
        return punchAction.action.actionMap.asset;
    }
}
