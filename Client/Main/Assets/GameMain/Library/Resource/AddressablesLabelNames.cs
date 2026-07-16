using UnityEngine;

namespace Dragon.Resource
{
    /// <summary>
    /// Addressables 标签名与启动预加载配置
    /// </summary>
    public static class AddressablesLabelNames
    {
        public const string Sprite = "Sprite";
        public const string Prefab = "Prefab";
        public const string UI = "UI";
        public const string Audio = "Audio";

        /// <summary>
        /// 启动预加载条目（标签 + 资源类型）。
        /// 新增启动预加载标签时，在此追加一项即可；下载列表会自动包含这些标签。
        /// </summary>
        public static readonly AddressablesBootstrapEntry[] BootstrapEntries =
        {
            // new AddressablesBootstrapEntry(Sprite, typeof(Sprite)),
            new AddressablesBootstrapEntry(Prefab, typeof(GameObject)),
            new AddressablesBootstrapEntry(UI, typeof(GameObject)),
            new AddressablesBootstrapEntry(Audio, typeof(AudioClip)),
        };
    }

    /// <summary>
    /// 启动阶段按标签预加载的配置项
    /// </summary>
    public readonly struct AddressablesBootstrapEntry
    {
        public string Label { get; }
        public System.Type AssetType { get; }

        public AddressablesBootstrapEntry(string label, System.Type assetType)
        {
            Label = label;
            AssetType = assetType;
        }
    }
}
