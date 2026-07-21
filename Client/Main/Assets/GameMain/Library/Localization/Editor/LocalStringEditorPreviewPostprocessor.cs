#if UNITY_EDITOR
using UnityEditor;

namespace Dragon.Localization.Editor
{
    internal sealed class LocalStringEditorPreviewPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (importedAssets[i].EndsWith("dragon_tblocalization.json"))
                {
                    LocalStringEditorPreview.InvalidateCache();
                    return;
                }
            }
        }
    }
}
#endif
