using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PopupConfirmData : PopupBaseData
{
    public Action onConfirm;
    public string content;
}

public class PopupConfirm : PopupBase<PopupConfirmData>
{
    // [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button confirmButton;
    private PopupConfirmData _data;
    
    protected override void OnOpen(PopupConfirmData data)
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