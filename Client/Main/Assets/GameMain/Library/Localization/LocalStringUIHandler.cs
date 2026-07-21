using Dragon.Localization;
using TMPro;
using UnityEngine;

[ExecuteAlways]
public class LocalStringUIHandler : MonoBehaviour
{
#if UNITY_EDITOR
    public static System.Action<LocalStringUIHandler> EditorRefreshCallback;
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

    private void OnValidate()
    {
        CacheTextComponents();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorRefreshCallback?.Invoke(this);
        }
#endif
    }

    public void Start()
    {
        CacheTextComponents();
        if (textMeshProUGUI == null && textMeshPro == null)
            return;

        if (string.IsNullOrEmpty(_localizationKey))
        {
            if (textMeshProUGUI != null && !string.IsNullOrEmpty(textMeshProUGUI.text))
                _localizationKey = textMeshProUGUI.text;
            else if (textMeshPro != null && !string.IsNullOrEmpty(textMeshPro.text))
                _localizationKey = textMeshPro.text;
        }

        SetText(_localizationKey);
        LocalManager.Instance.LanguageChanged += RefershText;
    }

    public void OnDestroy()
    {
        if (Application.isPlaying)
            LocalManager.Instance.LanguageChanged -= RefershText;
    }

    private void CacheTextComponents()
    {
        if (textMeshProUGUI == null)
            textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        if (textMeshProUGUI == null && textMeshPro == null)
            textMeshPro = GetComponent<TextMeshPro>();
    }

    private void SetText(string key)
    {
        _localizationKey = key ?? string.Empty;
        localString = _localizationKey;
        ApplyDisplayText(localString.Value);
    }

    public void SetText(LocalString localString)
    {
        SetText(localString.Key);
    }

    private void RefershText()
    {
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
