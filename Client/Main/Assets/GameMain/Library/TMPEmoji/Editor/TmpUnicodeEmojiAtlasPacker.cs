#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dragon.Library;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore;

namespace Dragon.Library.Editor
{
    /// <summary>
    /// 将「Unicode 十六进制命名」的散图 PNG 打成一张图集，并生成 TMP Sprite Asset（自动填 Unicode）。
    /// 用法：Tools/TMP/Pack Unicode Emoji PNGs → 系统对话框选择 PNG 文件夹 → 再选工程内输出路径。
    /// </summary>
    public static class TmpUnicodeEmojiAtlasPacker
    {
        private const string PackMenuPath = "Tools/TMP/Pack Unicode Emoji PNGs";
        private const string FillUnicodeMenuPath = "Tools/TMP/Fill Unicode From Sprite Names";
        private const int MaxAtlasSize = 4096;
        private const int Padding = 2;

        [MenuItem(PackMenuPath, false, 2000)]
        private static void PackFromFolderDialog()
        {
            string folder = EditorUtility.OpenFolderPanel("选择表情 PNG 文件夹", "", "");
            if (string.IsNullOrEmpty(folder))
                return;

            List<string> pngPaths = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories)
                .OrderBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (pngPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("TMP Emoji Packer", "所选文件夹下没有找到 PNG。", "OK");
                return;
            }

            string atlasPath = EditorUtility.SaveFilePanelInProject(
                "保存图集到工程",
                "ChatEmojiAtlas",
                "png",
                "选择图集在工程内的输出路径",
                "Assets");

            if (string.IsNullOrEmpty(atlasPath))
                return;

            try
            {
                PackAndCreateSpriteAsset(pngPaths, atlasPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("TMP Emoji Packer", "打包失败，详见 Console。\n" + e.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void PackAndCreateSpriteAsset(List<string> pngPaths, string atlasAssetPath)
        {
            var sourceTextures = new List<Texture2D>(pngPaths.Count);
            var spriteNames = new List<string>(pngPaths.Count);

            for (int i = 0; i < pngPaths.Count; i++)
            {
                string path = pngPaths[i];
                EditorUtility.DisplayProgressBar("TMP Emoji Packer", $"读取 {Path.GetFileName(path)}", (float)i / pngPaths.Count);

                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes, false))
                {
                    UnityEngine.Object.DestroyImmediate(tex);
                    throw new Exception("无法读取图片: " + path);
                }

                tex.name = Path.GetFileNameWithoutExtension(path);
                sourceTextures.Add(tex);
                spriteNames.Add(tex.name);
            }

            EditorUtility.DisplayProgressBar("TMP Emoji Packer", "PackTextures...", 0.85f);

            var atlas = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Rect[] uvs = atlas.PackTextures(sourceTextures.ToArray(), Padding, MaxAtlasSize, false);
            if (uvs == null || uvs.Length != sourceTextures.Count)
            {
                CleanupRuntimeTextures(sourceTextures, atlas);
                throw new Exception($"合图失败：图片过多或单图过大，试着减小单图尺寸，或把 MaxAtlasSize 调大（当前 {MaxAtlasSize}）。");
            }

            byte[] pngBytes = atlas.EncodeToPNG();
            string absoluteAtlasPath = Path.GetFullPath(atlasAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteAtlasPath) ?? "Assets");
            File.WriteAllBytes(absoluteAtlasPath, pngBytes);

            var spriteRects = new SpriteRect[uvs.Length];
            var nameFileIdPairs = new SpriteNameFileIdPair[uvs.Length];
            for (int i = 0; i < uvs.Length; i++)
            {
                Rect uv = uvs[i];
                var pixelRect = new Rect(
                    Mathf.Round(uv.x * atlas.width),
                    Mathf.Round(uv.y * atlas.height),
                    Mathf.Round(uv.width * atlas.width),
                    Mathf.Round(uv.height * atlas.height));

                GUID spriteId = GUID.Generate();
                spriteRects[i] = new SpriteRect
                {
                    name = spriteNames[i],
                    rect = pixelRect,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero,
                    spriteID = spriteId
                };
                nameFileIdPairs[i] = new SpriteNameFileIdPair(spriteNames[i], spriteId);
            }

            CleanupRuntimeTextures(sourceTextures, atlas);

            AssetDatabase.ImportAsset(atlasAssetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(atlasAssetPath) as TextureImporter;
            if (importer == null)
                throw new Exception("找不到 TextureImporter: " + atlasAssetPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.spritePixelsPerUnit = 100;
            importer.maxTextureSize = MaxAtlasSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // TextureImporter.spritesheet 已废弃，改用 ISpriteEditorDataProvider
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(nameFileIdPairs);
            dataProvider.Apply();
            importer.SaveAndReimport();

            EditorUtility.DisplayProgressBar("TMP Emoji Packer", "创建 TMP Sprite Asset...", 0.95f);

            Texture2D atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasAssetPath);
            if (atlasTexture == null)
                throw new Exception("图集导入失败: " + atlasAssetPath);

            string spriteAssetPath = Path.ChangeExtension(atlasAssetPath, ".asset");
            if (AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(spriteAssetPath) != null)
                AssetDatabase.DeleteAsset(spriteAssetPath);

            TMP_SpriteAsset spriteAsset = CreateTmpSpriteAsset(atlasTexture, spriteAssetPath);
            int mapped = ApplyUnicodeFromNames(spriteAsset);
            string supportPath = CreateOrUpdateSupportAsset(spriteNames);
            EditorUtility.SetDirty(spriteAsset);
            AssetDatabase.SaveAssets();

            Selection.activeObject = spriteAsset;
            EditorGUIUtility.PingObject(spriteAsset);

            EditorUtility.DisplayDialog(
                "TMP Emoji Packer",
                $"完成。\nPNG: {pngPaths.Count}\n图集: {atlasAssetPath}\nSprite Asset: {spriteAssetPath}\nSupport: {supportPath}\n已映射 Unicode: {mapped}\n\nSupport 已写入 Resources/TMP。请拖到聊天室 Emoji Support 字段；TMP Settings Default Sprite Asset 指到 Sprite Asset。",
                "OK");
        }

        private static string CreateOrUpdateSupportAsset(List<string> spriteNames)
        {
            var unicodes = new List<uint>(spriteNames.Count);
            var seen = new HashSet<uint>();
            for (int i = 0; i < spriteNames.Count; i++)
            {
                if (!TryParseUnicodeFromName(spriteNames[i], out uint unicode))
                {
                    Debug.LogWarning($"[TMP Emoji Packer] Support 跳过无法解析的文件名: {spriteNames[i]}");
                    continue;
                }

                if (seen.Add(unicode))
                {
                    unicodes.Add(unicode);
                }
            }

            unicodes.Sort();

            string supportPath = GetSupportAssetPath();
            EnsureSupportFolderExists();
            TmpEmojiSupportAsset support = AssetDatabase.LoadAssetAtPath<TmpEmojiSupportAsset>(supportPath);
            if (support == null)
            {
                support = ScriptableObject.CreateInstance<TmpEmojiSupportAsset>();
                AssetDatabase.CreateAsset(support, supportPath);
            }

            support.SetUnicodes(unicodes);
            EditorUtility.SetDirty(support);
            AssetDatabase.SaveAssets();
            // 强制写成可读 int 列表，避免 uint 二进制 blob 在部分环境下反序列化异常
            var so = new SerializedObject(support);
            SerializedProperty prop = so.FindProperty("_unicodes");
            prop.arraySize = unicodes.Count;
            for (int i = 0; i < unicodes.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).intValue = (int)unicodes[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return supportPath;
        }

        private const string SupportResourceDir = "Assets/Resources/TMP";
        private const string SupportAssetPath = SupportResourceDir + "/TmpEmojiSupport.asset";

        private static void EnsureSupportFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(SupportResourceDir))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "TMP");
            }
        }

        private static string GetSupportAssetPath()
        {
            return SupportAssetPath;
        }

        private static TMP_SpriteAsset CreateTmpSpriteAsset(Texture2D atlasTexture, string spriteAssetPath)
        {
            var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            AssetDatabase.CreateAsset(spriteAsset, spriteAssetPath);

            // version 的 setter 是 internal，用 SerializedObject 写 m_Version
            var so = new SerializedObject(spriteAsset);
            so.FindProperty("m_Version").stringValue = "1.1.0";
            so.ApplyModifiedPropertiesWithoutUndo();

            spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(Path.GetFileNameWithoutExtension(spriteAssetPath));
            spriteAsset.spriteSheet = atlasTexture;

            string texturePath = AssetDatabase.GetAssetPath(atlasTexture);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                .Select(x => x as Sprite)
                .Where(x => x != null)
                .OrderByDescending(x => x.rect.y)
                .ThenBy(x => x.rect.x)
                .ToArray();

            spriteAsset.spriteGlyphTable.Clear();
            spriteAsset.spriteCharacterTable.Clear();

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                // 与 TMP 自带 EmojiOne 一致：BearingX=0，BearingY≈高度*0.9，避免相对文字往左下偏
                float w = sprite.rect.width;
                float h = sprite.rect.height;
                var glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    metrics = CreateEmojiGlyphMetrics(w, h),
                    glyphRect = new GlyphRect(sprite.rect),
                    scale = 1.0f,
                    sprite = sprite
                };
                spriteAsset.spriteGlyphTable.Add(glyph);

                var character = new TMP_SpriteCharacter(0xFFFE, glyph)
                {
                    name = sprite.name,
                    scale = 1.0f
                };
                spriteAsset.spriteCharacterTable.Add(character);
            }

            Shader shader = Shader.Find("TextMeshPro/Sprite");
            if (shader == null)
                throw new Exception("找不到 Shader: TextMeshPro/Sprite");

            var material = new Material(shader);
            material.SetTexture(ShaderUtilities.ID_MainTex, atlasTexture);
            material.hideFlags = HideFlags.HideInHierarchy;
            spriteAsset.material = material;
            AssetDatabase.AddObjectToAsset(material, spriteAsset);

            spriteAsset.UpdateLookupTables();
            EditorUtility.SetDirty(spriteAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(spriteAssetPath);
            return AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(spriteAssetPath);
        }

        private static void CleanupRuntimeTextures(List<Texture2D> sources, Texture2D atlas)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] != null)
                    UnityEngine.Object.DestroyImmediate(sources[i]);
            }

            if (atlas != null)
                UnityEngine.Object.DestroyImmediate(atlas);
        }

        [MenuItem(FillUnicodeMenuPath, false, 2001)]
        private static void FillUnicodeMenu()
        {
            var asset = Selection.activeObject as TMP_SpriteAsset;
            if (asset == null)
            {
                EditorUtility.DisplayDialog("TMP Emoji Packer", "请先在 Project 里选中一个 TMP Sprite Asset。", "OK");
                return;
            }

            int mapped = ApplyUnicodeFromNames(asset);
            string supportPath = CreateOrUpdateSupportAsset(
                asset.spriteCharacterTable.Select(c => c != null ? c.name : string.Empty).ToList());
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "TMP Emoji Packer",
                $"已写入 Unicode：{mapped} 个。\nSupport: {supportPath}",
                "OK");
        }

        [MenuItem(FillUnicodeMenuPath, true)]
        private static bool FillUnicodeMenuValidate()
        {
            return Selection.activeObject is TMP_SpriteAsset;
        }

        /// <summary>EmojiOne 示例约为 115.6/128 ≈ 0.9。</summary>
        private const float BearingYRatio = 0.9f;

        private static GlyphMetrics CreateEmojiGlyphMetrics(float width, float height)
        {
            return new GlyphMetrics(
                width,
                height,
                0f,
                height * BearingYRatio,
                width);
        }

        /// <summary>
        /// 从 sprite 名解析 Unicode。支持：1f600、1F600、u1f600、U+1F600；
        /// 多段如 2764-fe0f 取主码点；含 200d 的 ZWJ 序列只取首个有效码点（TMP 单码点替换）。
        /// </summary>
        internal static int ApplyUnicodeFromNames(TMP_SpriteAsset spriteAsset)
        {
            if (spriteAsset?.spriteCharacterTable == null)
                return 0;

            int mapped = 0;
            for (int i = 0; i < spriteAsset.spriteCharacterTable.Count; i++)
            {
                TMP_SpriteCharacter ch = spriteAsset.spriteCharacterTable[i];
                if (ch == null || string.IsNullOrEmpty(ch.name) || ch.name == ".notdef")
                    continue;

                if (!TryParseUnicodeFromName(ch.name, out uint unicode))
                {
                    Debug.LogWarning($"[TMP Emoji Packer] 无法从名称解析 Unicode: {ch.name}");
                    continue;
                }

                ch.unicode = unicode;
                mapped++;
            }

            spriteAsset.SortGlyphTable();
            spriteAsset.UpdateLookupTables();
            return mapped;
        }

        private static readonly Regex HexToken = new Regex(
            @"^(?:u\+?|U\+?)?([0-9a-fA-F]{2,8})$",
            RegexOptions.Compiled);

        internal static bool TryParseUnicodeFromName(string name, out uint unicode)
        {
            unicode = 0;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // 去掉可能的前缀 emoji_ / char_
            string n = name.Trim();
            int lastSlash = Mathf.Max(n.LastIndexOf('/'), n.LastIndexOf('\\'));
            if (lastSlash >= 0)
                n = n.Substring(lastSlash + 1);

            string[] parts = n.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string token = part;
                Match m = HexToken.Match(token);
                if (!m.Success)
                    continue;

                string hex = m.Groups[1].Value;
                if (!uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint code))
                    continue;

                // 跳过变体选择符 / ZWJ，继续找主码点
                if (code == 0x200D || code == 0xFE0F || code == 0xFE0E)
                    continue;

                unicode = code;
                return true;
            }

            return false;
        }
    }
}
#endif
