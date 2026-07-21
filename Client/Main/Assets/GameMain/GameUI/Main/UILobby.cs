using UnityEngine;
using System.Collections;
using Dragon.GameUI;
using Dragon.Library;
using Dragon.Localization;
using Dragon.Logic;
using Dragon.Audio;
using UnityEngine.UI;
using TMPro;


public class UILobbyData : UIBaseData
{
}

public class UILobby : UIBase<UILobbyData>
{
    [SerializeField] private TextMeshProUGUI _testText;
    protected override void Init(UILobbyData data)
    {
        Debug.Log("UILobby Init");
        LocalizationHelper.SetText(_testText, "3");
    }

    protected override void Show(UILobbyData data)
    {
        Debug.Log("UILobby Show");
        AudioManager.Instance.PlayBgm("dungeon_ambience");
    }

    public override void Hide()
    {
        Debug.Log("UILobby Hide");
    }

    public void OnTest()
    {
        MapSceneManager.Instance.LoadSceneAndSpawnAsync(1);
        GameCenter.StateManager.ChangeState(GameStateCode.Loading, new GameLoadingParam
        {
            TargetState = GameStateCode.Fight,
            LoadingTime = 1f
        });
    }

    public void OnOpenDebugLog()
    {
        GameCenter.DebugLogManager.OpenPopup();
    }

    public void OnTestTut()
    {
        PopupManager.Instance.OpenPopup(
            "popup_tutorial_template",
            new PopupTutorialSlideData());
    }

    /// <summary>调试：在简体中文与英文之间切换。</summary>
    public void OnToggleLanguage()
    {
        LocalManager localManager = GameCenter.LocalManager;
        LanguageType nextLanguage = localManager.CurrentLanguage == LanguageType.English
            ? LanguageType.SimplifiedChinese
            : LanguageType.English;
        localManager.SwitchLanguage(nextLanguage);
        Debug.Log($"[Debug] Language switched to {nextLanguage}");
    }
}
