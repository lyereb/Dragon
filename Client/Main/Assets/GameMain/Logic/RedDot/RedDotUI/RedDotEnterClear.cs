using Dragon.Logic;
using UnityEngine;

namespace Dragon.GameUI
{
    /// <summary>
    /// 挂到按钮等入口：点击时标记对应红点已读（SeenUntilEnter / OncePerSession）。
    /// </summary>
    public class RedDotEnterClear : MonoBehaviour
    {
        [SerializeField] private string _key;

        public void SetKey(string key)
        {
            _key = key;
        }

        /// <summary>绑定 Button OnClick。</summary>
        public void OnClickClear()
        {
            if (string.IsNullOrEmpty(_key))
            {
                return;
            }

            RedDotManager.Instance?.MarkSeen(_key);
        }
    }
}
