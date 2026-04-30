using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Dragon.Resource
{
    /// <summary>
    /// 资源加载策略接口
    /// </summary>
    public interface IResourceLoadStrategy
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        T Load<T>(string path) where T : UnityEngine.Object;
        
        /// <summary>
        /// 异步加载资源
        /// </summary>
        ResourceRequest LoadAsync<T>(string path, Action<T> onComplete) where T : UnityEngine.Object;
    }

    /// <summary>
    /// 可释放资源接口，用于统一管理不同类型的资源句柄
    /// </summary>
    public interface IReleasableResource
    {
        void Release();
        bool IsValid { get; }
    }

    /// <summary>
    /// Addressables资源句柄包装器
    /// </summary>
    public class AddressableResourceHandle<T> : IReleasableResource where T : UnityEngine.Object
    {
        private AsyncOperationHandle<T> _handle;
        
        public AddressableResourceHandle(AsyncOperationHandle<T> handle)
        {
            _handle = handle;
        }
        
        public T Asset => _handle.Status == AsyncOperationStatus.Succeeded ? _handle.Result : null;
        
        public void Release()
        {
            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
            }
        }
        
        public bool IsValid => _handle.IsValid();
    }

    /// <summary>
    /// 基于Resources的资源加载策略
    /// </summary>
    public class ResourcesLoadStrategy : IResourceLoadStrategy
    {
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            return Resources.Load<T>(path);
        }

        public ResourceRequest LoadAsync<T>(string path, Action<T> onComplete) where T : UnityEngine.Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(path);
            request.completed += (asyncOp) =>
            {
                T resource = request.asset as T;
                onComplete?.Invoke(resource);
            };
            return request;
        }
    }

    /// <summary>
    /// 基于Addressables的资源加载策略
    /// </summary>
    public class AddressablesLoadStrategy : IResourceLoadStrategy
    {
        public T Load<T>(string address) where T : UnityEngine.Object
        {
            // Addressables不推荐直接同步加载，这里返回null并提示使用异步方法
            Debug.LogWarning("Addressables不支持直接同步加载资源，请使用异步加载方法");
            return null;
        }

        public ResourceRequest LoadAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    onComplete?.Invoke(op.Result);
                }
                else
                {
                    Debug.LogError($"Addressables加载资源失败: {address}, 错误: {op.OperationException}");
                    onComplete?.Invoke(null);
                }
            };
            
            // 创建一个包装ResourceRequest以便与现有系统兼容
            // 注意：这不是真正的ResourceRequest，仅用于保持接口一致性
            ResourceRequest request = new ResourceRequest();
            return request;
        }
    }
}