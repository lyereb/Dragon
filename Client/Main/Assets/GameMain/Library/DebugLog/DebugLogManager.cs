using System;
using System.Collections.Generic;
using System.Text;
using Dragon.GameUI;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 全局 Debug 日志缓冲，启动后常驻收集 Unity Console 输出。
    /// 需在 <see cref="GameCenter"/> 中 Init / Shutdown；弹窗依赖 Addressables 键 <see cref="PopupAddress"/>。
    /// </summary>
    public class DebugLogManager
    {
        public const string PopupAddress = "popup_debug_log";
        public const int DefaultMaxLines = 500;

        private static DebugLogManager _instance;

        public static DebugLogManager Instance => _instance ??= new DebugLogManager();

        private readonly List<string> _lines = new List<string>();
        private int _maxLines = DefaultMaxLines;
        private bool _initialized;

        public event Action Changed;

        public IReadOnlyList<string> Lines => _lines;

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
            _lines.Clear();
            Changed = null;
            _initialized = false;
        }

        public string GetFullText()
        {
            if (_lines.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < _lines.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(_lines[i]);
            }

            return builder.ToString();
        }

        public void Clear()
        {
            if (_lines.Count == 0)
            {
                return;
            }

            _lines.Clear();
            Changed?.Invoke();
        }

        public void OpenPopup()
        {
            GameCenter.PopupManager.OpenPopup<PopupDebugLogData>(PopupAddress, new PopupDebugLogData());
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            AddLine($"{GetLogPrefix(type)} {condition}");
        }

        private void AddLine(string line)
        {
            _lines.Add(line);
            if (_lines.Count > _maxLines)
            {
                _lines.RemoveRange(0, _lines.Count - _maxLines);
            }

            Changed?.Invoke();
        }

        private static string GetLogPrefix(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    return "[Error]";
                case LogType.Warning:
                    return "[Warn]";
                case LogType.Assert:
                    return "[Assert]";
                default:
                    return "[Log]";
            }
        }
    }
}
