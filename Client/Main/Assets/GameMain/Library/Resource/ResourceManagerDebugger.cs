#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Text;
using UnityEngine;

namespace Dragon.Resource
{
    /// <summary>
    /// ResourceManager的静态调试工具类
    /// 提供资源管理器的调试和监控功能
    /// </summary>
    public static class ResourceManagerDebugger
    {
        /// <summary>
        /// 打印资源管理器状态报告
        /// </summary>
        public static void PrintStatus()
        {
            var resourceManager = ResourceManager.Instance;
            var stats = resourceManager.GetStats();
            
            StringBuilder report = new StringBuilder();
            report.AppendLine("===== ResourceManager 状态报告 =====");
            report.AppendLine($"当前加载策略: {stats.CurrentStrategy}");
            report.AppendLine($"总缓存资源数: {stats.TotalCachedResources}");
            report.AppendLine($"跟踪的引用计数数: {stats.TrackedReferenceCounts}");
            report.AppendLine($"路径映射数: {stats.PathMappings}");
            report.AppendLine("================================");
            
            Debug.Log(report.ToString());
        }
        
        /// <summary>
        /// 验证所有缓存资源的有效性并清理无效资源
        /// </summary>
        public static void ValidateAndCleanAllResources()
        {
            var resourceManager = ResourceManager.Instance;
            int cleanedCount = resourceManager.ValidateAndCleanAllResources();
            Debug.Log($"资源验证完成，清理了 {cleanedCount} 个无效资源");
        }
        
        /// <summary>
        /// 获取资源管理器的完整报告
        /// </summary>
        /// <returns>格式化的报告字符串</returns>
        public static string GetFullReport()
        {
            var resourceManager = ResourceManager.Instance;
            var stats = resourceManager.GetStats();
            
            StringBuilder report = new StringBuilder();
            report.AppendLine("ResourceManager Report:");
            report.AppendLine($"  Strategy: {stats.CurrentStrategy}");
            report.AppendLine($"  Cached Resources: {stats.TotalCachedResources}");
            report.AppendLine($"  Tracked References: {stats.TrackedReferenceCounts}");
            report.AppendLine($"  Path Mappings: {stats.PathMappings}");
            
            return report.ToString();
        }
        
        /// <summary>
        /// 强制卸载未使用的资源
        /// </summary>
        public static void ForceUnloadUnusedAssets()
        {
            var resourceManager = ResourceManager.Instance;
            resourceManager.UnloadUnusedAssets();
            Debug.Log("已强制卸载未使用的资源");
        }
        
        /// <summary>
        /// 获取特定资源的详细信息
        /// </summary>
        /// <param name="path">资源路径</param>
        public static void PrintResourceInfo(string path)
        {
            var resourceManager = ResourceManager.Instance;
            int refCount = resourceManager.GetReferenceCount(path);
            Debug.Log($"资源 '{path}' 的引用计数: {refCount}");
        }
    }
}
#endif