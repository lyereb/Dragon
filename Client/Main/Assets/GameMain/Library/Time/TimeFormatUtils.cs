using System;

namespace Dragon.Library
{
    /// <summary>
    /// 时间格式化工具：用于秒数和常见时间文本互转展示。
    /// </summary>
    public static class TimeFormatUtils
    {
        private const int SecondPerMinute = 60;
        private const int SecondPerHour = 3600;
        private const int SecondPerDay = 86400;

        public struct TimeParts
        {
            public int Days;
            public int Hours;
            public int Minutes;
            public int Seconds;
            public long TotalSeconds;
        }

        /// <summary>
        /// 将秒数拆分为天/时/分/秒（时分秒为余数值）。
        /// </summary>
        public static TimeParts ToParts(long totalSeconds)
        {
            if (totalSeconds < 0)
            {
                totalSeconds = 0;
            }

            TimeParts parts = new TimeParts();
            parts.TotalSeconds = totalSeconds;

            parts.Days = (int)(totalSeconds / SecondPerDay);
            long remain = totalSeconds % SecondPerDay;

            parts.Hours = (int)(remain / SecondPerHour);
            remain %= SecondPerHour;

            parts.Minutes = (int)(remain / SecondPerMinute);
            parts.Seconds = (int)(remain % SecondPerMinute);
            return parts;
        }

        /// <summary>
        /// 将秒数格式化为 HH:mm:ss（总小时，不拆天）。
        /// </summary>
        public static string ToHHmmss(long totalSeconds)
        {
            if (totalSeconds < 0)
            {
                totalSeconds = 0;
            }

            long hours = totalSeconds / SecondPerHour;
            long remain = totalSeconds % SecondPerHour;
            long minutes = remain / SecondPerMinute;
            long seconds = remain % SecondPerMinute;
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// 将秒数格式化为 dd:HH:mm:ss（天与时分秒分离）。
        /// </summary>
        public static string ToDdHHmmss(long totalSeconds)
        {
            TimeParts p = ToParts(totalSeconds);
            return $"{p.Days:D2}:{p.Hours:D2}:{p.Minutes:D2}:{p.Seconds:D2}";
        }

        /// <summary>
        /// 将秒数格式化为完整中文样式：X天X小时X分X秒。
        /// </summary>
        public static string ToChineseDayTime(long totalSeconds)
        {
            TimeParts p = ToParts(totalSeconds);
            return $"{p.Days}天{p.Hours}小时{p.Minutes}分{p.Seconds}秒";
        }

        /// <summary>
        /// 智能中文格式：
        /// >=1天: X天X小时X分X秒
        /// >=1小时: X小时X分X秒
        /// >=1分钟: X分X秒
        /// <1分钟: X秒
        /// </summary>
        public static string ToChineseSmart(long totalSeconds)
        {
            TimeParts p = ToParts(totalSeconds);
            if (p.Days > 0)
            {
                return $"{p.Days}天{p.Hours}小时{p.Minutes}分{p.Seconds}秒";
            }

            if (p.Hours > 0)
            {
                return $"{p.Hours}小时{p.Minutes}分{p.Seconds}秒";
            }

            if (p.Minutes > 0)
            {
                return $"{p.Minutes}分{p.Seconds}秒";
            }

            return $"{p.Seconds}秒";
        }

        /// <summary>
        /// Smart format:
        /// >=1 day: <days>d HH:mm:ss
        /// <1天: HH:mm:ss
        /// </summary>
        public static string ToSmart(long totalSeconds)
        {
            TimeParts p = ToParts(totalSeconds);
            if (p.Days > 0)
            {
                return $"{p.Days}d {p.Hours:D2}:{p.Minutes:D2}:{p.Seconds:D2}";
            }

            return $"{p.Hours:D2}:{p.Minutes:D2}:{p.Seconds:D2}";
        }

        /// <summary>
        /// 按自定义模板格式化。支持占位符：
        /// {dd} {d} {hh} {h} {mm} {m} {ss} {s}
        /// </summary>
        public static string ToCustom(long totalSeconds, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return ToSmart(totalSeconds);
            }

            TimeParts p = ToParts(totalSeconds);
            return pattern
                .Replace("{dd}", p.Days.ToString("D2"))
                .Replace("{d}", p.Days.ToString())
                .Replace("{hh}", p.Hours.ToString("D2"))
                .Replace("{h}", p.Hours.ToString())
                .Replace("{mm}", p.Minutes.ToString("D2"))
                .Replace("{m}", p.Minutes.ToString())
                .Replace("{ss}", p.Seconds.ToString("D2"))
                .Replace("{s}", p.Seconds.ToString());
        }

        /// <summary>
        /// 从 TimeSpan 格式化为 HH:mm:ss（总小时，不拆天）。
        /// </summary>
        public static string ToHHmmss(TimeSpan span)
        {
            if (span.TotalSeconds < 0)
            {
                span = TimeSpan.Zero;
            }

            long totalSeconds = (long)Math.Floor(span.TotalSeconds);
            return ToHHmmss(totalSeconds);
        }
    }
}
