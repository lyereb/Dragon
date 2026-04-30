using UnityEngine;
using Dragon.GameUI;

public class UILoadingData : UIBaseData
{
    public string LoadingText;
}

public class UILoading : UIBase<UILoadingData>
{
    protected override void Init(UILoadingData data)
    {
        Debug.Log("UILoading Init: LoadingText = " + data.LoadingText);
    }

    protected override void Show(UILoadingData data)
    {
        Debug.Log("UILoading Show LoadingText = " + data.LoadingText);
    }

    public override void Hide()
    {
        Debug.Log("UILoading Hide");
    }
}
