#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dragon.Editor
{
    /// <summary>
    /// 监听 Config/Datas 下 xlsx 变更，可开关自动跑 Luban Gen。
    /// Excel 保存会触发多次事件，故做防抖；忽略 ~$ 临时锁文件。
    /// </summary>
    [InitializeOnLoad]
    public static class ConfigDatasAutoGenWatcher
    {
        private const string PrefKey = "Dragon.Editor.ConfigDatasAutoGen";
        private const string MenuPath = "Tools/Config/Auto Gen On Datas Change";
        private const double DebounceSeconds = 1.5;

        private static FileSystemWatcher _watcher;
        private static readonly object _lock = new object();
        private static bool _dirty;
        private static DateTime _lastChangeUtc = DateTime.MinValue;
        private static bool _isGenerating;

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PrefKey, false);
            private set => EditorPrefs.SetBool(PrefKey, value);
        }

        static ConfigDatasAutoGenWatcher()
        {
            EditorApplication.update += OnEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += StopWatcher;
            EditorApplication.quitting += StopWatcher;

            if (IsEnabled)
            {
                // Domain Reload 后延迟启动，等工程路径就绪
                EditorApplication.delayCall += StartWatcher;
            }
        }

        [MenuItem(MenuPath, false, 250)]
        private static void ToggleAutoGen()
        {
            IsEnabled = !IsEnabled;
            if (IsEnabled)
            {
                StartWatcher();
                Debug.Log($"[Config AutoGen] 已开启：监听 {GetDatasPath()}");
            }
            else
            {
                StopWatcher();
                Debug.Log("[Config AutoGen] 已关闭");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleAutoGenValidate()
        {
            Menu.SetChecked(MenuPath, IsEnabled);
            return true;
        }

        private static string GetDatasPath()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, "..", "..", "Config", "Datas"));
        }

        private static void StartWatcher()
        {
            StopWatcher();

            var datasPath = GetDatasPath();
            if (!Directory.Exists(datasPath))
            {
                Debug.LogWarning($"[Config AutoGen] Datas 目录不存在，无法监听: {datasPath}");
                return;
            }

            try
            {
                _watcher = new FileSystemWatcher(datasPath)
                {
                    Filter = "*.xlsx",
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnDatasChanged;
                _watcher.Created += OnDatasChanged;
                _watcher.Renamed += OnDatasRenamed;
                _watcher.Deleted += OnDatasChanged;

                Debug.Log($"[Config AutoGen] 正在监听: {datasPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Config AutoGen] 启动监听失败: {e.Message}");
                StopWatcher();
            }
        }

        private static void StopWatcher()
        {
            if (_watcher == null)
                return;

            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnDatasChanged;
            _watcher.Created -= OnDatasChanged;
            _watcher.Renamed -= OnDatasRenamed;
            _watcher.Deleted -= OnDatasChanged;
            _watcher.Dispose();
            _watcher = null;

            lock (_lock)
            {
                _dirty = false;
                _lastChangeUtc = DateTime.MinValue;
            }
        }

        private static void OnDatasRenamed(object sender, RenamedEventArgs e)
        {
            MarkDirty(e.FullPath);
            MarkDirty(e.OldFullPath);
        }

        private static void OnDatasChanged(object sender, FileSystemEventArgs e)
        {
            MarkDirty(e.FullPath);
        }

        private static void MarkDirty(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var fileName = Path.GetFileName(path);
            // Excel 锁文件 / 临时文件（勿在此线程调用 Unity API）
            if (fileName.StartsWith("~$", StringComparison.Ordinal) ||
                fileName.StartsWith(".", StringComparison.Ordinal))
                return;

            if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return;

            lock (_lock)
            {
                _dirty = true;
                _lastChangeUtc = DateTime.UtcNow;
            }
        }

        private static void OnEditorUpdate()
        {
            if (!IsEnabled || _isGenerating)
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            bool shouldRun;
            lock (_lock)
            {
                shouldRun = _dirty &&
                            (DateTime.UtcNow - _lastChangeUtc).TotalSeconds >= DebounceSeconds;
                if (shouldRun)
                {
                    _dirty = false;
                    _lastChangeUtc = DateTime.MinValue;
                }
            }

            if (!shouldRun)
                return;

            _isGenerating = true;
            try
            {
                Debug.Log("[Config AutoGen] 检测到 Datas 变更，开始自动 Gen...");
                AutoConfigProcessor.RunConfigGen(showDialogs: false);
            }
            finally
            {
                _isGenerating = false;
            }
        }
    }
}
#endif
