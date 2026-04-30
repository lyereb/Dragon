using UnityEngine;
using System.Collections;
using Dragon.GameUI;
using Dragon.Library;
using Dragon.Logic;
using UnityEngine.UI;
using Dragon.Audio;

public class UIFightData : UIBaseData
{
}

public class UIFight : UIBase<UIFightData>
{
    protected override void Init(UIFightData data)
    {
        Debug.Log("UIFight Init");
    }

    protected override void Show(UIFightData data)
    {
        Debug.Log("UIFight Show");
        AudioManager.Instance.PlayBgm("dungeon_battle");
    }

    public override void Hide()
    {
        Debug.Log("UIFight Hide");
    }

    public void OnTest()
    {
        MapSceneManager.Instance.QuitMap();
        GameCenter.StateManager.ChangeState(GameStateCode.Loading, new GameLoadingParam
        {
            TargetState = GameStateCode.Main,
            LoadingTime = 1f
        });
    }
}
