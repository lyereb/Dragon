using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 标记由 <see cref="PoolManager"/> 创建的对象，用于归还时识别所属池。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoolMember : MonoBehaviour
    {
        public string PoolId { get; internal set; }

        /// <summary>为 true 表示已由 <see cref="PoolManager.Get"/> 借出，尚未 <see cref="PoolManager.Release"/>。</summary>
        public bool IsLeased { get; internal set; }
    }
}
