using UnityEngine;

namespace Dragon.GameUI
{
    public class UIDefine
    {
        public static class UISortingType
        {
            public const int Background = 0;
            public const int Normal = 100;
            public const int Navigation = 200;
            public const int Popup = 300;
            public const int Tutorial = 400;
            public const int Notification = 500;
            public const int Debug = 1000;
        }
    }

    /// <summary>
    /// 非泛型根类型：供 UIManager 缓存、查找与 <see cref="Close"/> 使用；具体界面继承 <see cref="UIBase{TData}"/>。
    /// </summary>
    public abstract class UIBase : MonoBehaviour
    {
        public abstract void Hide();
        public void Close()
        {
            GameUIManager.Instance.CloseUI(this);
        }
    }

    /// <summary>
    /// TData 与界面一一对应；子类实现 <see cref="Init(TData)"/>（<paramref name="data"/> 允许为 null）。
    /// </summary>
    public abstract class UIBase<TData> : UIBase where TData : UIBaseData
    {
        internal void InitByManager(TData data)
        {
            Init(data);
        }

        internal void ShowByManager(TData data)
        {
            Show(data);
        }

        protected abstract void Init(TData data);
        protected abstract void Show(TData data);

    }

    public class UIBaseData
    {

    }
}