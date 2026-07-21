using Dragon.Localization;
using TMPro;
using UnityEngine;

[ExecuteAlways]
public class LocalizationUIHandler : MonoBehaviour
{
#if UNITY_EDITOR
    public static System.Action<LocalizationUIHandler> EditorRefreshCallback;
#endif

    [SerializeField] private string _localizationKey = string.Empty;

    private TextMeshProUGUI textMeshProUGUI;
    private TextMeshPro textMeshPro;
    private LocalString localString;

    public string LocalizationKey
    {
        get => _localizationKey;
        set => SetText(value);
    }

    private void Awake()
    {
        CacheTextComponents();
    }

    private void OnEnable()
    {
        CacheTextComponents();
        if (textMeshProUGUI == null && textMeshPro == null)
            return;

        TryMigrateKeyFromTmp();

        if (Application.isPlaying)
        {
            LocalManager.Instance.LanguageChanged += RefershText;
            RefershText();
            return;
        }

#if UNITY_EDITOR
        EditorRefreshCallback?.Invoke(this);
#endif
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            LocalManager.Instance.LanguageChanged -= RefershText;
    }

    private void OnValidate()
    {
        CacheTextComponents();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorRefreshCallback?.Invoke(this);
#endif
    }

    private void CacheTextComponents()
    {
        if (textMeshProUGUI == null)
            textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        if (textMeshProUGUI == null && textMeshPro == null)
            textMeshPro = GetComponent<TextMeshPro>();
    }

    private void TryMigrateKeyFromTmp()
    {
        if (!string.IsNullOrEmpty(_localizationKey))
            return;

        if (textMeshProUGUI != null && !string.IsNullOrEmpty(textMeshProUGUI.text))
            _localizationKey = textMeshProUGUI.text;
        else if (textMeshPro != null && !string.IsNullOrEmpty(textMeshPro.text))
            _localizationKey = textMeshPro.text;
    }

    public void SetText(string key)
    {
        _localizationKey = key ?? string.Empty;
        localString = _localizationKey;
        RefershText();
    }

    public void SetText(LocalString value)
    {
        SetText(value.Key);
    }

    private void RefershText()
    {
        if (localString == null)
            localString = _localizationKey;

        ApplyDisplayText(localString.Value);
    }

    private void ApplyDisplayText(string text)
    {
        if (textMeshProUGUI != null)
            textMeshProUGUI.text = text;
        else if (textMeshPro != null)
            textMeshPro.text = text;
    }
}
