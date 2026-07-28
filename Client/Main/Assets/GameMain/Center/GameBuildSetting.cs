using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 正式 / 测试环境配置（ScriptableObject，放在 Assets/Resources，会打进包）。
    /// 编辑器通过 Tools/Debug 菜单切换；运行时与出包均读此资源。
    /// </summary>
    [CreateAssetMenu(fileName = "GameBuildSetting", menuName = "Game/Build Setting")]
    public class GameBuildSetting : ScriptableObject
    {
        public const string AssetPath = "Assets/Resources/GameBuildSetting.asset";
        public const string ResourcesLoadPath = "GameBuildSetting";

        [SerializeField]
        [Tooltip("勾选 = 正式环境（隐藏 Debug UI 等）；不勾 = 测试环境。")]
        private bool _isProduction;

        private static GameBuildSetting _instance;

        /// <summary>是否按正式环境处理（隐藏 Debug UI 等）。</summary>
        public static bool IsProduction => Get().IsProductionValue;

        /// <summary>是否允许保留 Debug 专用对象 / 入口。</summary>
        public static bool AllowDebugUi => !IsProduction;

        public bool IsProductionValue => _isProduction;

        private static GameBuildSetting Get()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = Resources.Load<GameBuildSetting>(ResourcesLoadPath);

#if UNITY_EDITOR
            if (_instance == null)
            {
                _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<GameBuildSetting>(AssetPath);
            }
#endif

            if (_instance == null)
            {
                Debug.LogWarning("[GameBuildSetting] 未找到配置资源，默认按测试环境处理。请用 Tools/Debug 菜单创建。");
                _instance = CreateInstance<GameBuildSetting>();
                _instance._isProduction = false;
            }

            return _instance;
        }

#if UNITY_EDITOR
        /// <summary>编辑器获取或创建会进包的配置资源。</summary>
        public static GameBuildSetting GetOrCreateAsset()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameBuildSetting>(AssetPath);
            if (asset != null)
            {
                _instance = asset;
                return asset;
            }

            EnsureResourcesFolder();
            asset = CreateInstance<GameBuildSetting>();
            asset._isProduction = false;
            UnityEditor.AssetDatabase.CreateAsset(asset, AssetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            _instance = asset;
            Debug.Log($"[GameBuildSetting] 已创建配置资源：{AssetPath}");
            return asset;
        }

        public void SetIsProduction(bool isProduction)
        {
            if (_isProduction == isProduction)
            {
                return;
            }

            _isProduction = isProduction;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            _instance = this;
        }

        private static void EnsureResourcesFolder()
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }
        }
#endif
    }
}
