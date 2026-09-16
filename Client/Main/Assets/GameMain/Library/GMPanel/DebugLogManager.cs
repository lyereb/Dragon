using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Dragon.Library
{
    public enum DebugLogFilter
    {
        All,
        Log,
        Warning,
        Error,
    }

    public enum DebugLogChangeKind
    {
        Append,
        TrimHead,
        Clear,
    }

    public readonly struct DebugLogEntry
    {
        public readonly LogType Type;
        public readonly string Text;

        public DebugLogEntry(LogType type, string text)
        {
            Type = type;
            Text = text;
        }
    }

    /// <summary>
    /// 全局 Debug 日志缓冲，启动后常驻收集 Unity Console 输出。
    /// 由 <see cref="GMPanelManager"/> 持有并在 <see cref="GameCenter"/> 中 Init / Shutdown；展示由 <see cref="UIGMPanelDebugLog"/> 负责。
    /// </summary>
    public class DebugLogManager
    {
        public const int DefaultMaxLines = 500;

        private static DebugLogManager _instance;

        public static DebugLogManager Instance => _instance ??= new DebugLogManager();

        private readonly List<DebugLogEntry> _entries = new List<DebugLogEntry>();
        private int _maxLines = DefaultMaxLines;
        private bool _initialized;

        public int Count => _entries.Count;
        public int LastChangeCount { get; private set; }

        public event Action<DebugLogChangeKind> Changed;

        public void Init(int maxLines = DefaultMaxLines)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _maxLines = Mathf.Max(1, maxLines);
            Application.logMessageReceived += OnLogMessageReceived;
        }

        public void Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            Application.logMessageReceived -= OnLogMessageReceived;
            _entries.Clear();
            Changed = null;
            _initialized = false;
        }

        public DebugLogEntry GetEntry(int index)
        {
            return _entries[index];
        }

        public int GetFilteredCount(DebugLogFilter filter)
        {
            int count = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (MatchesFilter(_entries[i].Type, filter))
                {
                    count++;
                }
            }

            return count;
        }

        public bool TryGetFilteredEntry(int filteredIndex, DebugLogFilter filter, out DebugLogEntry entry, out int sourceIndex)
        {
            entry = default;
            sourceIndex = -1;

            if (filteredIndex < 0)
            {
                return false;
            }

            int current = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (!MatchesFilter(_entries[i].Type, filter))
                {
                    continue;
                }

                if (current == filteredIndex)
                {
                    entry = _entries[i];
                    sourceIndex = i;
                    return true;
                }

                current++;
            }

            return false;
        }

        public string GetFullText()
        {
            return GetDisplayText(DebugLogFilter.All);
        }

        public string GetDisplayText(DebugLogFilter filter)
        {
            if (_entries.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < _entries.Count; i++)
            {
                DebugLogEntry entry = _entries[i];
                if (!MatchesFilter(entry.Type, filter))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(FormatDisplayText(entry));
            }

            return builder.ToString();
        }

        public static string FormatDisplayText(in DebugLogEntry entry)
        {
            string text = EscapeRichText(entry.Text);
            switch (entry.Type)
            {
                case LogType.Warning:
                    return $"<color=#FFEB04>{text}</color>";
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return $"<color=#FF4444>{text}</color>";
                default:
                    return text;
            }
        }

        public void Clear()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            _entries.Clear();
            LastChangeCount = 0;
            Changed?.Invoke(DebugLogChangeKind.Clear);
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            AddEntry(type, $"{GetTimePrefix()} {condition}");
        }

        private void AddEntry(LogType type, string line)
        {
            _entries.Add(new DebugLogEntry(type, line));
            LastChangeCount = 1;

            if (_entries.Count > _maxLines)
            {
                int trimmedCount = _entries.Count - _maxLines;
                _entries.RemoveRange(0, trimmedCount);
                LastChangeCount = trimmedCount;
                Changed?.Invoke(DebugLogChangeKind.TrimHead);
                return;
            }

            Changed?.Invoke(DebugLogChangeKind.Append);
        }

        public static bool MatchesFilter(LogType type, DebugLogFilter filter)
        {
            switch (filter)
            {
                case DebugLogFilter.All:
                    return true;
                case DebugLogFilter.Log:
                    return type == LogType.Log;
                case DebugLogFilter.Warning:
                    return type == LogType.Warning;
                case DebugLogFilter.Error:
                    return type == LogType.Error
                        || type == LogType.Exception
                        || type == LogType.Assert;
                default:
                    return true;
            }
        }

        private static string GetTimePrefix()
        {
            return DateTime.Now.ToString("[HH:mm:ss]");
        }

        private static string EscapeRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text.Replace("<", "\u003c");
        }
    }
}
