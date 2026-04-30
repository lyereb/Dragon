using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupBase : MonoBehaviour
{
    public PopupBaseData CurrentData { get; private set; }
    // 对外统一入口：实例化预制体后由管理器调用。
    public void Open(PopupBaseData data)
    {
        CurrentData = data;
        gameObject.SetActive(true);
        OnOpen(data);
    }

    // 子类重写这个方法处理自己的打开逻辑。
    protected virtual void OnOpen(PopupBaseData data)
    {
    }

    // 对外统一关闭入口。
    public void Close()
    {
        OnClose();
        InvokeCloseCallback();
        Destroy(gameObject);
    }

    protected virtual void OnClose()
    {
    }

    private void InvokeCloseCallback()
    {
        if (CurrentData?.closeCallback == null)
            return;

        try
        {
            CurrentData.closeCallback.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Popup closeCallback 执行异常: {ex}");
        }
        finally
        {
            // 防止重复关闭时重复回调
            CurrentData.closeCallback = null;
        }
    }
}

public class PopupBaseData
{
    public string popupName;
    public Action closeCallback;
}

// 推荐给具体弹窗继承，避免在每个子类里手动做类型转换。
public abstract class PopupBase<TData> : PopupBase where TData : PopupBaseData
{
    protected sealed override void OnOpen(PopupBaseData data)
    {
        if (data is not TData typedData)
        {
            Debug.LogError($"{GetType().Name} 打开失败，数据类型不匹配：期望 {typeof(TData).Name}，实际 {(data == null ? "null" : data.GetType().Name)}");
            return;
        }

        OnOpen(typedData);
    }

    protected abstract void OnOpen(TData data);
}