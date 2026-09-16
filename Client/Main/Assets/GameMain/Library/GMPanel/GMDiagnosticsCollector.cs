using System.Text;
using Dragon.Logic;
using Dragon.Resource;
using UnityEngine;
using UnityEngine.Profiling;

namespace Dragon.Library
{
    public static class GMDiagnosticsCollector
    {
        public static string BuildReport(float fps, float frameTimeMs)
        {
            var builder = new StringBuilder(1024);

            AppendSection(builder, "Performance");
            AppendLine(builder, "FPS", fps > 0f ? fps.ToString("F1") : "-");
            AppendLine(builder, "Frame Time", frameTimeMs > 0f ? $"{frameTimeMs:F1} ms" : "-");
            AppendLine(builder, "Target FPS", Application.targetFrameRate.ToString());
            AppendLine(builder, "GC Memory", FormatBytes(System.GC.GetTotalMemory(false)));

            long allocated = Profiler.GetTotalAllocatedMemoryLong();
            if (allocated > 0)
            {
                AppendLine(builder, "Allocated", FormatBytes(allocated));
            }

            AppendSection(builder, "Environment");
            AppendLine(builder, "Platform", Application.platform.ToString());
            AppendLine(builder, "Version", Application.version);
            AppendLine(builder, "Unity", Application.unityVersion);
            AppendLine(builder, "Build", GameBuildSetting.IsProduction ? "Production" : "Test");
            AppendLine(builder, "Uptime", $"{Time.realtimeSinceStartup:F0}s");
            AppendLine(builder, "Focused", Application.isFocused ? "Yes" : "No");

            AppendSection(builder, "Device / Network");
            AppendLine(builder, "Device", SystemInfo.deviceModel);
            AppendLine(builder, "OS", SystemInfo.operatingSystem);
            AppendLine(builder, "System RAM", GetSystemMemoryText());
            AppendLine(builder, "GPU", SystemInfo.graphicsDeviceName);
            AppendLine(builder, "Network", GetNetworkReachabilityText());
            AppendLine(builder, "Persistent Path", Application.persistentDataPath);

            AppendSection(builder, "Game");
            AppendLine(builder, "Game State", GetGameStateName());
            AppendLine(builder, "Language", GetLanguageName());

            AppendSection(builder, "UI / Resources");
            AppendLine(builder, "Resource Cache", GetResourceCacheCount().ToString());

            AppendSection(builder, "Screen");
            AppendLine(builder, "Resolution", $"{Screen.width} x {Screen.height}");
            AppendLine(builder, "Safe Area", FormatRect(Screen.safeArea));

            return builder.ToString();
        }

        private static void AppendSection(StringBuilder builder, string title)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append("<b>");
            builder.Append(title);
            builder.Append("</b>\n");
        }

        private static void AppendLine(StringBuilder builder, string label, string value)
        {
            builder.Append(label);
            builder.Append(": ");
            builder.Append(string.IsNullOrEmpty(value) ? "-" : value);
            builder.Append('\n');
        }

        private static string GetGameStateName()
        {
            BaseState state = GameCenter.StateManager?.GetCurState();
            return state != null ? state.Name : "-";
        }

        private static string GetLanguageName()
        {
            return GameCenter.LocalSettingManager != null
                ? GameCenter.LocalSettingManager.Language.ToString()
                : "-";
        }

        private static int GetResourceCacheCount()
        {
            return GameCenter.ResourceManager != null
                ? GameCenter.ResourceManager.GetCachedResourceCount()
                : 0;
        }

        private static string GetSystemMemoryText()
        {
            int memoryMb = SystemInfo.systemMemorySize;
            return memoryMb > 0 ? $"{memoryMb} MB" : "-";
        }

        private static string GetNetworkReachabilityText()
        {
            switch (Application.internetReachability)
            {
                case NetworkReachability.NotReachable:
                    return "None";
                case NetworkReachability.ReachableViaCarrierDataNetwork:
                    return "Carrier";
                case NetworkReachability.ReachableViaLocalAreaNetwork:
                    return "Local";
                default:
                    return Application.internetReachability.ToString();
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }

            if (bytes < 1024 * 1024)
            {
                return $"{bytes / 1024f:F1} KB";
            }

            return $"{bytes / (1024f * 1024f):F1} MB";
        }

        private static string FormatRect(Rect rect)
        {
            return $"({rect.x:F0}, {rect.y:F0}, {rect.width:F0}, {rect.height:F0})";
        }
    }
}
