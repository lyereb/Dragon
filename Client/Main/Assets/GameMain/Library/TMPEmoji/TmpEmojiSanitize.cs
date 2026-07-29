using System.Text;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 按打图集生成的 <see cref="TmpEmojiSupportAsset"/> 白名单，去掉未支持的 Unicode emoji。
    /// </summary>
    public static class TmpEmojiSanitize
    {
        private const string SupportResourcesPath = "TMP/TmpEmojiSupport";

        private static TmpEmojiSupportAsset _supportAsset;
        private static bool _resourcesLoadAttempted;

        public static TmpEmojiSupportAsset SupportAsset
        {
            get
            {
                EnsureSupportAsset();
                return _supportAsset;
            }
        }

        public static void SetSupportAsset(TmpEmojiSupportAsset supportAsset)
        {
            _supportAsset = supportAsset;
            // 显式清空时允许之后再走 Resources 兜底
            _resourcesLoadAttempted = supportAsset != null;
        }

        /// <summary>
        /// 移除未支持的 emoji 相关码点；普通文字与白名单内 emoji 保留。
        /// 未配置白名单时不做过滤。
        /// </summary>
        public static string RemoveUnsupported(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            TmpEmojiSupportAsset support = SupportAsset;
            if (support == null || support.Count <= 0)
            {
                return text;
            }

            var builder = new StringBuilder(text.Length);
            bool changed = false;

            for (int i = 0; i < text.Length;)
            {
                // 不完整代理对：输入过程中先原样保留，避免把正在组合的表情打掉
                if (char.IsHighSurrogate(text[i]))
                {
                    if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]))
                    {
                        builder.Append(text[i]);
                        i += 1;
                        continue;
                    }

                    int codePoint = char.ConvertToUtf32(text, i);
                    if (ShouldKeepCodePoint(support, codePoint))
                    {
                        builder.Append(text, i, 2);
                    }
                    else
                    {
                        changed = true;
                    }

                    i += 2;
                    continue;
                }

                if (char.IsLowSurrogate(text[i]))
                {
                    builder.Append(text[i]);
                    i += 1;
                    continue;
                }

                int bmp = text[i];
                if (ShouldKeepCodePoint(support, bmp))
                {
                    builder.Append(text[i]);
                }
                else
                {
                    changed = true;
                }

                i += 1;
            }

            return changed ? builder.ToString() : text;
        }

        private static void EnsureSupportAsset()
        {
            if (_supportAsset != null)
            {
                return;
            }

            if (_resourcesLoadAttempted)
            {
                return;
            }

            _resourcesLoadAttempted = true;
            _supportAsset = Resources.Load<TmpEmojiSupportAsset>(SupportResourcesPath);
        }

        private static bool ShouldKeepCodePoint(TmpEmojiSupportAsset support, int codePoint)
        {
            if (!IsEmojiRelated(codePoint))
            {
                return true;
            }

            // 变体选择符 / ZWJ / 肤色：单独不保留
            if (IsEmojiModifierOrJoiner(codePoint))
            {
                return false;
            }

            return support.Contains((uint)codePoint);
        }

        private static bool IsEmojiModifierOrJoiner(int codePoint)
        {
            return codePoint == 0x200D
                || codePoint == 0xFE0E
                || codePoint == 0xFE0F
                || codePoint == 0x20E3
                || (codePoint >= 0x1F3FB && codePoint <= 0x1F3FF);
        }

        private static bool IsEmojiRelated(int codePoint)
        {
            if (IsEmojiModifierOrJoiner(codePoint))
            {
                return true;
            }

            if (codePoint >= 0x1F1E6 && codePoint <= 0x1F1FF)
            {
                return true;
            }

            if (codePoint >= 0x1F300 && codePoint <= 0x1FAFF)
            {
                return true;
            }

            if (codePoint >= 0x2600 && codePoint <= 0x27BF)
            {
                return true;
            }

            if (codePoint >= 0x2300 && codePoint <= 0x23FF)
            {
                return true;
            }

            if (codePoint >= 0x2B50 && codePoint <= 0x2B55)
            {
                return true;
            }

            if (codePoint == 0x3030 || codePoint == 0x303D || codePoint == 0x3297 || codePoint == 0x3299)
            {
                return true;
            }

            return false;
        }
    }
}
