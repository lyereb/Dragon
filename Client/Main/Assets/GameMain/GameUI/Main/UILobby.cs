using UnityEngine;
using System.Collections;
using Dragon.GameUI;
using Dragon.Library;
using Dragon.Logic;
using Dragon.Audio;
using UnityEngine.UI;

public class UILobbyData : UIBaseData
{
}

public class UILobby : UIBase<UILobbyData>
{
    protected override void Init(UILobbyData data)
    {
        Debug.Log("UILobby Init");
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
}
