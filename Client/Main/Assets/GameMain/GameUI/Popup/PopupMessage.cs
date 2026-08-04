using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PopupMessageData : PopupBaseData
{
    public Action onConfirm;
    public string content;
}

public class PopupMessage : PopupBase<PopupMessageData>
{
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button confirmButton;
    private PopupMessageData _data;

    protected override void OnOpen(PopupMessageData data)
    {
        _data = data;
        contentText.text = data.content;
    }

    public void OnConfirm()
    {
        _data.onConfirm?.Invoke();
        Close();
    }
}
