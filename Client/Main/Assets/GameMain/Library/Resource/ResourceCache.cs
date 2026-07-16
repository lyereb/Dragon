using System.Collections.Generic;
using UnityEngine;

namespace Dragon.Resource
{
    /// <summary>
    /// 资源缓存接口，定义缓存操作的标准方法。
    /// 键由 ResourceManager 生成（地址 + 类型），本层按不透明字符串处理。
    /// </summary>
    public interface IResourceCache
    {
        /// <summary>
        /// 尝试从缓存中获取资源
        /// </summary>
        bool TryGetResource<T>(string cacheKey, out T resource) where T : UnityEngine.Object;
        
        /// <summary>
        /// 将资源添加到缓存中
        /// </summary>
        void AddResource<T>(string cacheKey, T resource) where T : UnityEngine.Object;
        
        /// <summary>
        /// 从缓存中移除资源
        /// </summary>
        void RemoveResource(string cacheKey);
        
        /// <summary>
        /// 验证资源是否有效
        /// </summary>
        bool IsResourceValid(string cacheKey);
        
        /// <summary>
        /// 清理所有无效资源
        /// </summary>
        int CleanInvalidResources();
        
        /// <summary>
        /// 获取缓存资源数量
        /// </summary>
        int GetCachedResourceCount();

        /// <summary>
        /// 清空全部缓存
        /// </summary>
        void Clear();
    }
    
    /// <summary>
    /// 资源缓存实现类，封装了缓存的具体逻辑
    /// </summary>
    public class ResourceCache : IResourceCache
    {
        private readonly Dictionary<string, UnityEngine.Object> _cachedResources = new Dictionary<string, UnityEngine.Object>();
        private readonly Dictionary<string, float> _lastValidationTimes = new Dictionary<string, float>();
        private const float VALIDATION_INTERVAL = 10f;
        
        public bool TryGetResource<T>(string cacheKey, out T resource) where T : UnityEngine.Object
        {
            resource = null;
            
            if (_cachedResources.ContainsKey(cacheKey))
            {
                if (IsResourceValid(cacheKey))
                {
                    resource = _cachedResources[cacheKey] as T;
                    _lastValidationTimes[cacheKey] = Time.time;
                    return resource != null;
                }
                else
                {
                    RemoveResource(cacheKey);
                }
            }
            
            return false;
        }
        
        public void AddResource<T>(string cacheKey, T resource) where T : UnityEngine.Object
        {
            if (resource != null)
            {
                _cachedResources[cacheKey] = resource;
                _lastValidationTimes[cacheKey] = Time.time;
            }
        }
        
        public void RemoveResource(string cacheKey)
        {
            _cachedResources.Remove(cacheKey);
            _lastValidationTimes.Remove(cacheKey);
        }
        
        public bool IsResourceValid(string cacheKey)
        {
            if (!_cachedResources.ContainsKey(cacheKey))
                return false;

            UnityEngine.Object resource = _cachedResources[cacheKey];
            
            if (resource == null)
                return false;

            if (resource is GameObject go && go == null)
                return false;

            if (_lastValidationTimes.ContainsKey(cacheKey))
            {
                float lastValidationTime = _lastValidationTimes[cacheKey];
                if (Time.time - lastValidationTime > VALIDATION_INTERVAL)
                {
                    _lastValidationTimes[cacheKey] = Time.time;
                }
            }
            else
            {
                _lastValidationTimes[cacheKey] = Time.time;
            }

            return true;
        }
        
        public int CleanInvalidResources()
        {
            List<string> invalidResources = new List<string>();
            
            foreach (var kvp in _cachedResources)
            {
                if (!IsResourceValid(kvp.Key))
                {
                    invalidResources.Add(kvp.Key);
                }
            }
            
            foreach (string cacheKey in invalidResources)
            {
                RemoveResource(cacheKey);
            }
            
            return invalidResources.Count;
        }
        
        public int GetCachedResourceCount()
        {
            return _cachedResources.Count;
        }

        public void Clear()
        {
            _cachedResources.Clear();
            _lastValidationTimes.Clear();
        }
    }
}