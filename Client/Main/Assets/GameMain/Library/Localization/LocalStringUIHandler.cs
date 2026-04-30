using Dragon.Localization;
using TMPro;
using UnityEngine;

public class LocalStringUIHandler : MonoBehaviour
{
    private TextMeshProUGUI textMeshProUGUI;
    private TextMeshPro textMeshPro;
    private LocalString localString;

    public void Start()
    {
        textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        if(textMeshProUGUI == null) textMeshPro = GetComponent<TextMeshPro>();
        if (textMeshProUGUI == null && textMeshPro == null)
            return;

        // UI界面文本多语言处理
        if (localString == null &&textMeshProUGUI != null && textMeshProUGUI.text != null)
            localString = textMeshProUGUI.text;
        else if (localString == null && textMeshPro != null && textMeshPro.text != null)
            localString = textMeshPro.text;
        else if (localString == null)
            localString = "";
        
        SetText(localString.Key);

        LocalManager.Instance.LanguageChanged += RefershText;
    }

    public void OnDestroy()
    {
        LocalManager.Instance.LanguageChanged -= RefershText;
    }

    // UI设置文本, UI界面配置语言表Id，Start的时候自动处理
    private void SetText(string key)
    {
        localString = key;

        if (textMeshProUGUI != null)
            textMeshProUGUI.text = localString.Value;
        else if (textMeshPro != null)
            textMeshPro.text = localString.Value;
    }

    // 代码调用设置文本方法，限定传入参数类型为LocalString,防止文本key、value混淆
    public void SetText(LocalString localString)
    {
        SetText(localString.Key);
    }

    // 语言切换时刷新文本,组件自动处理
    private void RefershText()
    {
        if (textMeshProUGUI != null)
            textMeshProUGUI.text = localString.Value;
        else if (textMeshPro != null)
            textMeshPro.text = localString.Value;
    }
}
