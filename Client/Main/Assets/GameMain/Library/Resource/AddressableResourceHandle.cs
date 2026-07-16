using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Dragon.Resource
{
    /// <summary>
    /// 可释放资源接口，用于统一管理 Addressables 资源句柄
    /// </summary>
    public interface IReleasableResource
    {
        void Release();
        bool IsValid { get; }
    }

    /// <summary>
    /// Addressables 资源句柄包装器
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
}
