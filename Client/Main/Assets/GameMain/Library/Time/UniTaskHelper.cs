using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// UniTask 定时调度封装：Delay / Interval / NextFrame / EveryFrame。
    /// 取消方式：handle.Cancel() / CancelByOwner / Unity Owner 销毁自动取消。
    /// </summary>
    public static class UniTaskHelper
    {
        private static readonly Dictionary<object, List<Handle>> _ownerHandles = new Dictionary<object, List<Handle>>(32);

        #region //对外 API

        /// <summary>
        /// 延时执行一次。
        /// </summary>
        public static Handle Delay(
            float seconds,
            Action callback,
            object owner = null,
            bool useUnscaledTime = false)
        {
            if (callback == null)
            {
                Debug.LogWarning("[UniTaskHelper] Delay callback is null.");
                return Handle.Empty;
            }

            if (seconds < 0f)
            {
                seconds = 0f;
            }

            var handle = CreateHandle(owner);
            RunDelayAsync(handle, seconds, callback, useUnscaledTime).Forget();
            return handle;
        }

        /// <summary>
        /// 定频重复执行，直到 Cancel。
        /// </summary>
        public static Handle Interval(
            float seconds,
            Action callback,
            object owner = null,
            bool useUnscaledTime = false)
        {
            if (callback == null)
            {
                Debug.LogWarning("[UniTaskHelper] Interval callback is null.");
                return Handle.Empty;
            }

            if (seconds <= 0f)
            {
                seconds = 0.001f;
            }

            var handle = CreateHandle(owner);
            RunIntervalAsync(handle, seconds, callback, useUnscaledTime).Forget();
            return handle;
        }

        /// <summary>
        /// 下一帧执行一次。
        /// </summary>
        public static Handle NextFrame(Action callback, object owner = null)
        {
            if (callback == null)
            {
                Debug.LogWarning("[UniTaskHelper] NextFrame callback is null.");
                return Handle.Empty;
            }

            var handle = CreateHandle(owner);
            RunNextFrameAsync(handle, callback).Forget();
            return handle;
        }

        /// <summary>
        /// 每帧执行，直到 Cancel。
        /// </summary>
        public static Handle EveryFrame(Action callback, object owner = null)
        {
            if (callback == null)
            {
                Debug.LogWarning("[UniTaskHelper] EveryFrame callback is null.");
                return Handle.Empty;
            }

            var handle = CreateHandle(owner);
            RunEveryFrameAsync(handle, callback, null, false).Forget();
            return handle;
        }

        /// <summary>
        /// 每帧执行（带 deltaTime），直到 Cancel。
        /// </summary>
        public static Handle EveryFrame(
            Action<float> callback,
            object owner = null,
            bool useUnscaledTime = false)
        {
            if (callback == null)
            {
                Debug.LogWarning("[UniTaskHelper] EveryFrame delta callback is null.");
                return Handle.Empty;
            }

            var handle = CreateHandle(owner);
            RunEveryFrameAsync(handle, null, callback, useUnscaledTime).Forget();
            return handle;
        }

        /// <summary>
        /// 取消指定定时任务。
        /// </summary>
        public static bool Cancel(Handle handle)
        {
            if (handle == null || handle.IsEmpty)
            {
                return false;
            }

            return handle.Cancel();
        }

        /// <summary>
        /// 取消某个 owner 下的全部任务。
        /// </summary>
        public static int CancelByOwner(object owner)
        {
            if (owner == null)
            {
                return 0;
            }

            if (!_ownerHandles.TryGetValue(owner, out var list) || list == null || list.Count == 0)
            {
                return 0;
            }

            int cancelled = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var handle = list[i];
                if (handle == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (handle.Cancel())
                {
                    cancelled++;
                }
            }

            _ownerHandles.Remove(owner);
            return cancelled;
        }

        #endregion

        #region //内部调度

        private static Handle CreateHandle(object owner)
        {
            CancellationTokenSource cts;

            // Unity 对象销毁时自动取消
            if (owner is Component component && component != null)
            {
                cts = CancellationTokenSource.CreateLinkedTokenSource(component.GetCancellationTokenOnDestroy());
            }
            else if (owner is GameObject go && go != null)
            {
                cts = CancellationTokenSource.CreateLinkedTokenSource(go.GetCancellationTokenOnDestroy());
            }
            else
            {
                cts = new CancellationTokenSource();
            }

            var handle = new Handle(cts, owner);
            BindOwner(owner, handle);
            return handle;
        }

        private static void BindOwner(object owner, Handle handle)
        {
            if (owner == null || handle == null || handle.IsEmpty)
            {
                return;
            }

            if (!_ownerHandles.TryGetValue(owner, out var list))
            {
                list = new List<Handle>(4);
                _ownerHandles[owner] = list;
            }

            list.Add(handle);
        }

        private static void UnbindOwner(Handle handle)
        {
            if (handle == null || handle.Owner == null)
            {
                return;
            }

            if (!_ownerHandles.TryGetValue(handle.Owner, out var list) || list == null)
            {
                return;
            }

            list.Remove(handle);
            if (list.Count == 0)
            {
                _ownerHandles.Remove(handle.Owner);
            }
        }

        private static async UniTaskVoid RunDelayAsync(
            Handle handle,
            float seconds,
            Action callback,
            bool useUnscaledTime)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(seconds),
                    ignoreTimeScale: useUnscaledTime,
                    cancellationToken: handle.Token);

                if (handle.IsCancelled)
                {
                    return;
                }

                InvokeSafe(callback, handle);
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            finally
            {
                handle.Complete();
            }
        }

        private static async UniTaskVoid RunIntervalAsync(
            Handle handle,
            float seconds,
            Action callback,
            bool useUnscaledTime)
        {
            try
            {
                while (!handle.IsCancelled)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(seconds),
                        ignoreTimeScale: useUnscaledTime,
                        cancellationToken: handle.Token);

                    if (handle.IsCancelled)
                    {
                        break;
                    }

                    InvokeSafe(callback, handle);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            finally
            {
                handle.Complete();
            }
        }

        private static async UniTaskVoid RunNextFrameAsync(Handle handle, Action callback)
        {
            try
            {
                await UniTask.Yield(PlayerLoopTiming.Update, handle.Token);

                if (handle.IsCancelled)
                {
                    return;
                }

                InvokeSafe(callback, handle);
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            finally
            {
                handle.Complete();
            }
        }

        private static async UniTaskVoid RunEveryFrameAsync(
            Handle handle,
            Action callback,
            Action<float> deltaCallback,
            bool useUnscaledTime)
        {
            try
            {
                while (!handle.IsCancelled)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, handle.Token);

                    if (handle.IsCancelled)
                    {
                        break;
                    }

                    if (callback != null)
                    {
                        InvokeSafe(callback, handle);
                    }
                    else if (deltaCallback != null)
                    {
                        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                        InvokeSafe(deltaCallback, dt, handle);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            finally
            {
                handle.Complete();
            }
        }

        private static void InvokeSafe(Action callback, Handle handle)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UniTaskHelper] Task execute exception, owner:{handle?.Owner}, msg:{e}");
                handle?.Cancel();
            }
        }

        private static void InvokeSafe(Action<float> callback, float dt, Handle handle)
        {
            try
            {
                callback?.Invoke(dt);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UniTaskHelper] Task execute exception, owner:{handle?.Owner}, msg:{e}");
                handle?.Cancel();
            }
        }

        #endregion

        /// <summary>
        /// 定时任务句柄：单次取消用 Cancel；批量取消用 CancelByOwner。
        /// </summary>
        public sealed class Handle : IDisposable
        {
            public static readonly Handle Empty = new Handle(null, null);

            private CancellationTokenSource _cts;
            private bool _completed;

            internal Handle(CancellationTokenSource cts, object owner)
            {
                _cts = cts;
                Owner = owner;
            }

            public object Owner { get; }

            public bool IsEmpty => ReferenceEquals(this, Empty);

            public bool IsCancelled
            {
                get
                {
                    if (IsEmpty || _completed)
                    {
                        return true;
                    }

                    if (_cts == null)
                    {
                        return true;
                    }

                    try
                    {
                        return _cts.IsCancellationRequested;
                    }
                    catch (ObjectDisposedException)
                    {
                        return true;
                    }
                }
            }

            public bool IsCompleted => _completed || IsEmpty;

            internal CancellationToken Token
            {
                get
                {
                    if (IsEmpty || _cts == null)
                    {
                        return new CancellationToken(canceled: true);
                    }

                    return _cts.Token;
                }
            }

            /// <summary>
            /// 请求取消；CTS 由异步收尾 Complete 再 Dispose，避免与 await 竞态。
            /// </summary>
            public bool Cancel()
            {
                if (IsEmpty || _completed)
                {
                    return false;
                }

                bool didCancel = false;
                if (_cts != null)
                {
                    try
                    {
                        if (!_cts.IsCancellationRequested)
                        {
                            _cts.Cancel();
                            didCancel = true;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // ignore
                    }
                }

                UnbindOwner(this);
                return didCancel;
            }

            internal void Complete()
            {
                if (_completed || IsEmpty)
                {
                    return;
                }

                _completed = true;
                UnbindOwner(this);

                if (_cts != null)
                {
                    try
                    {
                        if (!_cts.IsCancellationRequested)
                        {
                            _cts.Cancel();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // ignore
                    }

                    try
                    {
                        _cts.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // ignore
                    }

                    _cts = null;
                }
            }

            public void Dispose()
            {
                Cancel();
            }
        }
    }
}
