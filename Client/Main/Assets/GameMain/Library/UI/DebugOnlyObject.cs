using Dragon.Library;
using UnityEngine;

namespace Dragon.GameUI
{
    /// <summary>
    /// 正式环境下 Awake 销毁自身；测试环境保留。
    /// 判定见 <see cref="GameBuildSetting"/>；挂在 Debug 按钮等节点即可。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Debug Only Object")]
    public sealed class DebugOnlyObject : MonoBehaviour
    {
        private void Awake()
        {
            if (GameBuildSetting.IsProduction)
            {
                Destroy(gameObject);
            }
        }
    }
}
