using UnityEngine;
using System.Collections;
using Dragon.GameUI;
using Dragon.Logic;
using UnityEngine.UI;

public class UIItemData : UIBaseData
{
    public string ItemName;
}

public class UIItem : UIBase<UIItemData>
{
    protected override void Init(UIItemData data)
    {
        Debug.Log("UIItem Init: ItemName = " + data.ItemName);
    }

    protected override void Show(UIItemData data)
    {
        Debug.Log("UIItem Show ItemName = " + data.ItemName);
    }

    public override void Hide()
    {
        Debug.Log("UIItem Hide");
    }
}
