using System.Collections.Generic;
using UnityEngine;

namespace Dragon.Resource
{
    /// <summary>
    /// 资源缓存接口，定义缓存操作的标准方法
    /// </summary>
    public interface IResourceCache
    {
        /// <summary>
        /// 尝试从缓存中获取资源
        /// </summary>
        bool TryGetResource<T>(string path, out T resource) where T : UnityEngine.Object;
        
        /// <summary>
        /// 将资源添加到缓存中
        /// </summary>
        void AddResource<T>(string path, T resource) where T : UnityEngine.Object;
        
        /// <summary>
        /// 从缓存中移除资源
        /// </summary>
        void RemoveResource(string path);
        
        /// <summary>
        /// 验证资源是否有效
        /// </summary>
        bool IsResourceValid(string path);
        
        /// <summary>
        /// 清理所有无效资源
        /// </summary>
        int CleanInvalidResources();
        
        /// <summary>
        /// 获取缓存资源数量
        /// </summary>
        int GetCachedResourceCount();
    }
    
    /// <summary>
    /// 资源缓存实现类，封装了缓存的具体逻辑
    /// </summary>
    public class ResourceCache : IResourceCache
    {
        private readonly Dictionary<string, UnityEngine.Object> _cachedResources = new Dictionary<string, UnityEngine.Object>();
        private readonly Dictionary<string, float> _lastValidationTimes = new Dictionary<string, float>();
        private const float VALIDATION_INTERVAL = 10f;
        
        public bool TryGetResource<T>(string path, out T resource) where T : UnityEngine.Object
        {
            resource = null;
            
            if (_cachedResources.ContainsKey(path))
            {
                if (IsResourceValid(path))
                {
                    resource = _cachedResources[path] as T;
                    _lastValidationTimes[path] = Time.time;
                    return resource != null;
                }
                else
                {
                    RemoveResource(path);
                }
            }
            
            return false;
        }
        
        public void AddResource<T>(string path, T resource) where T : UnityEngine.Object
        {
            if (resource != null)
            {
                _cachedResources[path] = resource;
                _lastValidationTimes[path] = Time.time;
            }
        }
        
        public void RemoveResource(string path)
        {
            _cachedResources.Remove(path);
            _lastValidationTimes.Remove(path);
        }
        
        public bool IsResourceValid(string path)
        {
            if (!_cachedResources.ContainsKey(path))
                return false;

            UnityEngine.Object resource = _cachedResources[path];
            
            if (resource == null)
                return false;

            if (resource is GameObject go && go == null)
                return false;

            if (_lastValidationTimes.ContainsKey(path))
            {
                float lastValidationTime = _lastValidationTimes[path];
                if (Time.time - lastValidationTime > VALIDATION_INTERVAL)
                {
                    _lastValidationTimes[path] = Time.time;
                }
            }
            else
            {
                _lastValidationTimes[path] = Time.time;
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
            
            foreach (string path in invalidResources)
            {
                RemoveResource(path);
            }
            
            return invalidResources.Count;
        }
        
        public int GetCachedResourceCount()
        {
            return _cachedResources.Count;
        }
    }
}