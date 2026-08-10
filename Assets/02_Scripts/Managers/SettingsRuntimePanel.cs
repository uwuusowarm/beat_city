using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SettingsRuntimePanel : MonoBehaviour
{
    public static SettingsRuntimePanel ActivePanel { get; private set; }
    private enum SettingsTab
    {
        Player,
        Enemy,
        Ranged
    }

    [Header("Panel")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    [SerializeField] private bool showOnStart;
    [SerializeField] private Rect windowRect = new Rect(20f, 20f, 540f, 640f);

    private bool _isVisible;
    private SettingsTab _activeTab;
    private Vector2 _scroll;
    private int _windowId;

    private string _statusMessage = string.Empty;
    private Color _statusColor = Color.white;

    private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public;
    private const float BaseWindowWidth = 540f;
    private const float BaseWindowHeight = 640f;

    private GUIStyle _labelStyle;
    private GUIStyle _statusStyle;
    private GUIStyle _textFieldStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _toggleStyle;
    private GUIStyle _categoryButtonStyle;
    private GUIStyle _resizeHandleStyle;

    private float _labelWidth;
    private float _fieldWidth;
    private float _tabButtonHeight;
    private float _saveButtonHeight;
    private float _categoryButtonHeight;
    private float _categoryButtonWidth;
    private float _scrollHeight;

    private const float MinWindowWidth = 420f;
    private const float MinWindowHeight = 360f;
    private const float ResizeHandleSize = 18f;
    private const int MaxCategoryButtonsPerRow = 4;
    private const float AutoSliderMinSpan = 1f;
    private const float AutoSliderPaddingFactor = 1.5f;

    private bool _isResizing;
    private Vector2 _resizeStartMouse;
    private Vector2 _resizeStartSize;

    private bool _cursorStateSaved;
    private bool _prevCursorVisible;
    private CursorLockMode _prevCursorLockMode;

    private readonly Dictionary<Type, List<CategoryData>> _categoriesByType = new Dictionary<Type, List<CategoryData>>();
    private readonly Dictionary<SettingsTab, int> _selectedCategoryIndexByTab = new Dictionary<SettingsTab, int>();

    private class CategoryData
    {
        public string Name;
        public readonly List<FieldInfo> Fields = new List<FieldInfo>();
    }

    private void Awake()
    {
        ActivePanel = this;
        _isVisible = showOnStart;
        _windowId = GetInstanceID();

        foreach (SettingsTab tab in Enum.GetValues(typeof(SettingsTab)))
        {
            _selectedCategoryIndexByTab[tab] = 0;
        }

        if (_isVisible)
        {
            ActivateCursorForPanel();
        }
    }

    private void Update()
    {
        if (IsTogglePressed())
        {
            SetPanelVisible(!_isVisible);
        }
    }

    private void OnDisable()
    {
        if (ActivePanel == this)
        {
            ActivePanel = null;
        }

        if (_cursorStateSaved)
        {
            RestoreCursorState();
        }
    }

    public static bool IsMouseOverVisiblePanel()
    {
        SettingsRuntimePanel panel = ActivePanel;
        if (panel == null || !panel._isVisible)
        {
            return false;
        }

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return false;
        }

        Vector2 screenPosition = mouse.position.ReadValue();
        Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        return panel.windowRect.Contains(guiPosition);
#elif ENABLE_LEGACY_INPUT_MANAGER
        Vector2 screenPosition = Input.mousePosition;
        Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        return panel.windowRect.Contains(guiPosition);
#else
        return false;
#endif
    }

    private bool IsTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        if (Enum.TryParse(toggleKey.ToString(), out Key key))
        {
            return keyboard[key].wasPressedThisFrame;
        }

        return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(toggleKey);
#else
        return false;
#endif
    }

    private void OnGUI()
    {
        if (!_isVisible)
        {
            return;
        }

        HandleWindowResizeInput();
        windowRect = GUI.Window(_windowId, windowRect, DrawWindow, "Settings Runtime Panel");
        DrawResizeHandle();
    }

    private void DrawWindow(int windowId)
    {
        UpdateResponsiveLayout();

        GUILayout.BeginVertical();

        DrawTabButtons();
        DrawSelectedSettingsEditor();
        DrawSaveRow();

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
    }

    private void DrawTabButtons()
    {
        GUILayout.BeginHorizontal();

        DrawTabButton(SettingsTab.Player, "Player");
        DrawTabButton(SettingsTab.Enemy, "Enemy");

        GUILayout.EndHorizontal();
    }

    private void DrawTabButton(SettingsTab tab, string label)
    {
        bool wasActive = _activeTab == tab;
        GUI.backgroundColor = wasActive ? new Color(0.45f, 0.9f, 0.45f) : Color.white;

        if (GUILayout.Button(label, _buttonStyle, GUILayout.Height(_tabButtonHeight)))
        {
            if (_activeTab != tab)
            {
                _activeTab = tab;
                _scroll = Vector2.zero;
            }
        }

        GUI.backgroundColor = Color.white;
    }

    private void DrawSelectedSettingsEditor()
    {
        ScriptableObject settings = GetActiveSettings();

        if (settings == null)
        {
            GUILayout.Space(8f);
            GUILayout.Label($"No settings asset found for '{_activeTab}'.", _labelStyle);
            return;
        }

        GUILayout.Space(8f);
        GUILayout.Label($"Active Set: {settings.name}", _labelStyle);
        GUILayout.Space(4f);

        List<CategoryData> categories = GetOrCreateCategories(settings.GetType());
        DrawCategoryButtons(categories);

        int selectedCategoryIndex = GetSelectedCategoryIndex(categories.Count);
        if (categories.Count > 0)
        {
            GUILayout.Space(3f);
            GUILayout.Label($"Category: {categories[selectedCategoryIndex].Name}", _labelStyle);
        }

        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(_scrollHeight));
        DrawFields(settings, categories, selectedCategoryIndex);
        GUILayout.EndScrollView();
    }

    private ScriptableObject GetActiveSettings()
    {
        GameSettingsProvider provider = GameSettingsProvider.Instance;

        switch (_activeTab)
        {
            case SettingsTab.Player:
                if (provider != null && provider.PlayerSettings != null)
                    return provider.PlayerSettings;
                return Resources.Load<PlayerSettings>("PlayerSettings");

            case SettingsTab.Enemy:
                if (provider != null && provider.EnemySettings != null)
                    return provider.EnemySettings;
                return Resources.Load<EnemySettings>("EnemySettings");

            case SettingsTab.Ranged:
                if (provider != null && provider.EnemySettings != null)
                    return provider.EnemySettings;
                return Resources.Load<EnemySettings>("EnemySettings");

            default:
                return null;
        }
    }

    private void DrawFields(ScriptableObject settings, List<CategoryData> categories, int selectedCategoryIndex)
    {
        if (categories.Count == 0)
        {
            GUILayout.Label("No editable fields found.", _labelStyle);
            return;
        }

        CategoryData category = categories[selectedCategoryIndex];
        foreach (FieldInfo field in category.Fields)
        {
            DrawSingleField(settings, field);
        }
    }

    private static string NicifyFieldName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        int start = 0;
        if (name.Length > 2 && name[0] == 'm' && name[1] == '_')
            start = 2;
        else if (name[0] == '_')
            start = 1;
        else if (name.Length > 1 && name[0] == 'k' && char.IsUpper(name[1]))
            start = 1;

        StringBuilder builder = new StringBuilder(name.Length + 8);
        for (int i = start; i < name.Length; i++)
        {
            char current = name[i];
            if (i > start)
            {
                char previous = name[i - 1];
                bool startsWord = (char.IsUpper(current) && !char.IsUpper(previous)) ||
                                  (char.IsDigit(current) && !char.IsDigit(previous));
                if (startsWord)
                    builder.Append(' ');
            }

            builder.Append(i == start ? char.ToUpperInvariant(current) : current);
        }

        return builder.ToString();
    }

    private void DrawSingleField(ScriptableObject settings, FieldInfo field)
    {
        object currentValue = field.GetValue(settings);
        string label = NicifyFieldName(field.Name);

        GUILayout.BeginHorizontal();
        GUILayout.Label(label, _labelStyle, GUILayout.Width(_labelWidth));

        Type type = field.FieldType;
        RangeAttribute range = field.GetCustomAttribute<RangeAttribute>();

        if (type == typeof(int))
        {
            int parsed = DrawIntControl((int)currentValue, range);
            if (parsed != (int)currentValue)
                field.SetValue(settings, parsed);
        }
        else if (type == typeof(float))
        {
            float parsed = DrawFloatControl((float)currentValue, range);
            if (Math.Abs(parsed - (float)currentValue) > 0.0001f)
                field.SetValue(settings, parsed);
        }
        else if (type == typeof(bool))
        {
            bool newValue = GUILayout.Toggle((bool)currentValue, string.Empty, _toggleStyle, GUILayout.Width(24f));
            if (newValue != (bool)currentValue)
                field.SetValue(settings, newValue);
        }
        else if (type == typeof(Vector3))
        {
            Vector3 updated = DrawVector3Field((Vector3)currentValue);
            if (updated != (Vector3)currentValue)
                field.SetValue(settings, updated);
        }
        else if (type == typeof(LayerMask))
        {
            int maskValue = ((LayerMask)currentValue).value;
            int parsedMask = DrawIntField(maskValue);
            if (parsedMask != maskValue)
                field.SetValue(settings, (LayerMask)parsedMask);
        }
        else
        {
            GUILayout.Label($"({type.Name})", _labelStyle, GUILayout.Width(_fieldWidth));
        }

        GUILayout.EndHorizontal();
    }

    private int DrawIntControl(int value, RangeAttribute range)
    {
        GetIntSliderRange(value, range, out int min, out int max);

        float sliderValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(_fieldWidth * 0.62f));
        int clampedSlider = Mathf.RoundToInt(sliderValue);
        string raw = GUILayout.TextField(clampedSlider.ToString(), _textFieldStyle, GUILayout.Width(_fieldWidth * 0.36f));

        if (int.TryParse(raw, out int parsed))
        {
            return Mathf.Clamp(parsed, min, max);
        }

        return clampedSlider;
    }

    private float DrawFloatControl(float value, RangeAttribute range)
    {
        GetFloatSliderRange(value, range, out float min, out float max);

        float sliderValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(_fieldWidth * 0.62f));
        string raw = GUILayout.TextField(sliderValue.ToString("0.###", CultureInfo.InvariantCulture), _textFieldStyle, GUILayout.Width(_fieldWidth * 0.36f));

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
        {
            return Mathf.Clamp(parsed, min, max);
        }

        return sliderValue;
    }

    private void GetIntSliderRange(int value, RangeAttribute range, out int min, out int max)
    {
        if (range != null)
        {
            min = Mathf.RoundToInt(range.min);
            max = Mathf.RoundToInt(range.max);
        }
        else
        {
            float autoPadding = Mathf.Max(AutoSliderMinSpan, Mathf.Abs(value) * AutoSliderPaddingFactor);
            min = Mathf.FloorToInt(value - autoPadding);
            max = Mathf.CeilToInt(value + autoPadding);
        }

        if (max <= min)
        {
            max = min + 1;
        }
    }

    private void GetFloatSliderRange(float value, RangeAttribute range, out float min, out float max)
    {
        if (range != null)
        {
            min = range.min;
            max = range.max;
        }
        else
        {
            float autoPadding = Mathf.Max(AutoSliderMinSpan, Mathf.Abs(value) * AutoSliderPaddingFactor);
            min = value - autoPadding;
            max = value + autoPadding;
        }

        if (max <= min)
        {
            max = min + 0.01f;
        }
    }

    private int DrawIntField(int value)
    {
        string raw = GUILayout.TextField(value.ToString(), _textFieldStyle, GUILayout.Width(_fieldWidth));
        return int.TryParse(raw, out int parsed) ? parsed : value;
    }

    private float DrawFloatField(float value)
    {
        string raw = GUILayout.TextField(value.ToString(CultureInfo.InvariantCulture), _textFieldStyle, GUILayout.Width(_fieldWidth));
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : value;
    }

    private Vector3 DrawVector3Field(Vector3 value)
    {
        string raw = GUILayout.TextField($"{value.x.ToString(CultureInfo.InvariantCulture)},{value.y.ToString(CultureInfo.InvariantCulture)},{value.z.ToString(CultureInfo.InvariantCulture)}", _textFieldStyle, GUILayout.Width(_fieldWidth));
        string[] parts = raw.Split(',');

        if (parts.Length == 3 &&
            float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
        {
            return new Vector3(x, y, z);
        }

        return value;
    }

    private void DrawSaveRow()
    {
        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Current Set", _buttonStyle, GUILayout.Height(_saveButtonHeight)))
        {
            SaveCurrentSettings();
        }

        GUI.contentColor = _statusColor;
        GUILayout.Label(_statusMessage, _statusStyle, GUILayout.Width(_fieldWidth + 80f));
        GUI.contentColor = Color.white;

        GUILayout.EndHorizontal();
    }

    private void SaveCurrentSettings()
    {
        ScriptableObject settings = GetActiveSettings();
        if (settings == null)
        {
            SetStatus("No active settings asset to save.", Color.yellow);
            return;
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        SetStatus($"Saved: {settings.name}", new Color(0.45f, 0.9f, 0.45f));
#else
        SetStatus("Save is only available in the Unity Editor.", Color.yellow);
#endif
    }

    private void SetStatus(string message, Color color)
    {
        _statusMessage = message;
        _statusColor = color;
    }

    private void SetPanelVisible(bool visible)
    {
        if (_isVisible == visible)
        {
            return;
        }

        _isVisible = visible;
        if (_isVisible)
        {
            ActivateCursorForPanel();
        }
        else
        {
            RestoreCursorState();
        }
    }

    private void ActivateCursorForPanel()
    {
        if (!_cursorStateSaved)
        {
            _prevCursorVisible = Cursor.visible;
            _prevCursorLockMode = Cursor.lockState;
            _cursorStateSaved = true;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void RestoreCursorState()
    {
        Cursor.visible = _prevCursorVisible;
        Cursor.lockState = _prevCursorLockMode;
        _cursorStateSaved = false;
    }

    private List<CategoryData> GetOrCreateCategories(Type settingsType)
    {
        if (_categoriesByType.TryGetValue(settingsType, out List<CategoryData> existing))
        {
            return existing;
        }

        List<CategoryData> categories = BuildCategories(settingsType);
        _categoriesByType[settingsType] = categories;
        return categories;
    }

    private List<CategoryData> BuildCategories(Type settingsType)
    {
        FieldInfo[] fields = settingsType.GetFields(FieldFlags);
        List<CategoryData> categories = new List<CategoryData>();

        string currentCategoryName = "General";
        CategoryData currentCategory = CreateCategory(categories, currentCategoryName);

        foreach (FieldInfo field in fields)
        {
            HeaderAttribute header = field.GetCustomAttribute<HeaderAttribute>();
            if (header != null && !string.IsNullOrWhiteSpace(header.header))
            {
                currentCategoryName = header.header.Trim();
                currentCategory = FindOrCreateCategory(categories, currentCategoryName);
            }

            currentCategory.Fields.Add(field);
        }

        return categories;
    }

    private static CategoryData CreateCategory(List<CategoryData> categories, string name)
    {
        CategoryData category = new CategoryData { Name = name };
        categories.Add(category);
        return category;
    }

    private static CategoryData FindOrCreateCategory(List<CategoryData> categories, string name)
    {
        for (int i = 0; i < categories.Count; i++)
        {
            if (categories[i].Name == name)
            {
                return categories[i];
            }
        }

        return CreateCategory(categories, name);
    }

    private void DrawCategoryButtons(List<CategoryData> categories)
    {
        if (categories.Count <= 1)
        {
            return;
        }

        int selectedCategoryIndex = GetSelectedCategoryIndex(categories.Count);
        for (int i = 0; i < categories.Count; i += MaxCategoryButtonsPerRow)
        {
            GUILayout.BeginHorizontal();

            int end = Mathf.Min(i + MaxCategoryButtonsPerRow, categories.Count);
            for (int buttonIndex = i; buttonIndex < end; buttonIndex++)
            {
                bool isActive = selectedCategoryIndex == buttonIndex;
                GUI.backgroundColor = isActive ? new Color(0.45f, 0.75f, 0.95f) : Color.white;

                if (GUILayout.Button(categories[buttonIndex].Name, _categoryButtonStyle, GUILayout.Width(_categoryButtonWidth), GUILayout.Height(_categoryButtonHeight)))
                {
                    _selectedCategoryIndexByTab[_activeTab] = buttonIndex;
                    _scroll = Vector2.zero;
                }
            }

            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
    }

    private int GetSelectedCategoryIndex(int categoryCount)
    {
        if (!_selectedCategoryIndexByTab.TryGetValue(_activeTab, out int selectedCategoryIndex))
        {
            selectedCategoryIndex = 0;
        }

        if (categoryCount <= 0)
        {
            return 0;
        }

        int clamped = Mathf.Clamp(selectedCategoryIndex, 0, categoryCount - 1);
        _selectedCategoryIndexByTab[_activeTab] = clamped;
        return clamped;
    }

    private void DrawResizeHandle()
    {
        Rect resizeRect = GetResizeHandleRect();
        GUI.Box(resizeRect, "◢", _resizeHandleStyle);
    }

    private void HandleWindowResizeInput()
    {
        Event currentEvent = Event.current;
        if (currentEvent == null)
        {
            return;
        }

        Rect resizeRect = GetResizeHandleRect();

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && resizeRect.Contains(currentEvent.mousePosition))
        {
            _isResizing = true;
            _resizeStartMouse = currentEvent.mousePosition;
            _resizeStartSize = new Vector2(windowRect.width, windowRect.height);
            currentEvent.Use();
        }
        else if (_isResizing && currentEvent.type == EventType.MouseDrag)
        {
            Vector2 delta = currentEvent.mousePosition - _resizeStartMouse;
            windowRect.width = Mathf.Max(MinWindowWidth, _resizeStartSize.x + delta.x);
            windowRect.height = Mathf.Max(MinWindowHeight, _resizeStartSize.y + delta.y);
            currentEvent.Use();
        }
        else if (_isResizing && currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
        {
            _isResizing = false;
            currentEvent.Use();
        }
    }

    private Rect GetResizeHandleRect()
    {
        return new Rect(
            windowRect.x + windowRect.width - ResizeHandleSize - 2f,
            windowRect.y + windowRect.height - ResizeHandleSize - 2f,
            ResizeHandleSize,
            ResizeHandleSize);
    }

    private void UpdateResponsiveLayout()
    {
        float widthScale = Mathf.Clamp(windowRect.width / BaseWindowWidth, 0.75f, 1.8f);
        float heightScale = Mathf.Clamp(windowRect.height / BaseWindowHeight, 0.75f, 1.8f);
        float scale = Mathf.Min(widthScale, heightScale);

        int fontSize = Mathf.RoundToInt(13f * scale);
        int statusFontSize = Mathf.RoundToInt(12f * scale);

        _labelStyle ??= new GUIStyle(GUI.skin.label);
        _statusStyle ??= new GUIStyle(GUI.skin.label);
        _textFieldStyle ??= new GUIStyle(GUI.skin.textField);
        _buttonStyle ??= new GUIStyle(GUI.skin.button);
        _toggleStyle ??= new GUIStyle(GUI.skin.toggle);
        _categoryButtonStyle ??= new GUIStyle(GUI.skin.button);
        _resizeHandleStyle ??= new GUIStyle(GUI.skin.box);

        _labelStyle.fontSize = fontSize;
        _statusStyle.fontSize = statusFontSize;
        _statusStyle.wordWrap = true;
        _textFieldStyle.fontSize = fontSize;
        _buttonStyle.fontSize = fontSize;
        _toggleStyle.fontSize = fontSize;
        _categoryButtonStyle.fontSize = Mathf.Max(12, fontSize);
        _resizeHandleStyle.fontSize = Mathf.Max(12, fontSize);
        _resizeHandleStyle.alignment = TextAnchor.MiddleCenter;

        _tabButtonHeight = Mathf.Clamp(28f * scale, 22f, 44f);
        _saveButtonHeight = Mathf.Clamp(28f * scale, 22f, 44f);
        _categoryButtonHeight = Mathf.Clamp(32f * scale, 28f, 52f);

        float contentWidth = Mathf.Max(300f, windowRect.width - 28f);
        _categoryButtonWidth = Mathf.Max(80f, (contentWidth - 12f) / MaxCategoryButtonsPerRow);
        _labelWidth = Mathf.Clamp(contentWidth * 0.46f, 130f, 360f);
        _fieldWidth = Mathf.Max(140f, contentWidth - _labelWidth - 12f);

        float categoryHeight = _categoryButtonHeight;
        float reservedHeight = _tabButtonHeight + _saveButtonHeight + categoryHeight + (72f * scale);
        _scrollHeight = Mathf.Max(140f, windowRect.height - reservedHeight);
    }
}