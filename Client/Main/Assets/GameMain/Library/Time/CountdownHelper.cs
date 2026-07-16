using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 倒计时辅助类：基于 UniTaskHelper，固定 1 秒 Interval 递减。
    /// </summary>
    public static class CountdownHelper
    {
        private const float TickIntervalSeconds = 1f;

        private static readonly List<CountdownHandle> _handles = new List<CountdownHandle>(64);
        private static readonly Dictionary<object, List<CountdownHandle>> _ownerHandles = new Dictionary<object, List<CountdownHandle>>(32);

        /// <summary>
        /// 创建并启动一个倒计时。
        /// </summary>
        /// <param name="durationSeconds">总时长（秒），按向上取整作为初始剩余整秒</param>
        /// <param name="onTick">每次 tick 回调，参数为剩余整秒</param>
        /// <param name="onCompleted">倒计时结束回调</param>
        /// <param name="owner">作用域对象（UI/State/系统模块）</param>
        /// <param name="useUnscaledTime">是否使用 unscaled 时间</param>
        /// <param name="emitTickOnStart">是否创建后立即触发一次 tick</param>
        public static CountdownHandle StartCountdown(
            float durationSeconds,
            Action<int> onTick,
            Action onCompleted = null,
            object owner = null,
            bool useUnscaledTime = true,
            bool emitTickOnStart = true)
        {
            int remainingSeconds = durationSeconds <= 0f ? 0 : Mathf.CeilToInt(durationSeconds);
            var handle = new CountdownHandle(remainingSeconds, onTick, onCompleted, owner, useUnscaledTime);
            _handles.Add(handle);
            BindOwner(owner, handle);
            handle.Start(emitTickOnStart);
            return handle;
        }

        public static int CancelByOwner(object owner)
        {
            if (owner == null || !_ownerHandles.TryGetValue(owner, out var list) || list == null || list.Count == 0)
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

                if (!handle.IsDisposed)
                {
                    handle.Cancel();
                    cancelled++;
                }
            }

            _ownerHandles.Remove(owner);
            return cancelled;
        }

        public static void CancelAll()
        {
            for (int i = _handles.Count - 1; i >= 0; i--)
            {
                var handle = _handles[i];
                if (handle != null && !handle.IsDisposed)
                {
                    handle.Cancel();
                }
            }

            _handles.Clear();
            _ownerHandles.Clear();
        }

        private static void BindOwner(object owner, CountdownHandle handle)
        {
            if (owner == null || handle == null)
            {
                return;
            }

            if (!_ownerHandles.TryGetValue(owner, out var list))
            {
                list = new List<CountdownHandle>(4);
                _ownerHandles[owner] = list;
            }

            list.Add(handle);
        }

        private static void UnbindHandle(CountdownHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            _handles.Remove(handle);
            if (handle.Owner == null)
            {
                return;
            }

            if (!_ownerHandles.TryGetValue(handle.Owner, out var list))
            {
                return;
            }

            list.Remove(handle);
            if (list.Count == 0)
            {
                _ownerHandles.Remove(handle.Owner);
            }
        }

        public sealed class CountdownHandle
        {
            private readonly Action<int> _onTick;
            private readonly Action _onCompleted;
            private readonly bool _useUnscaledTime;

            private UniTaskHelper.Handle _tickHandle = UniTaskHelper.Handle.Empty;
            private int _remainingSeconds;
            private bool _isPaused;

            internal CountdownHandle(
                int remainingSeconds,
                Action<int> onTick,
                Action onCompleted,
                object owner,
                bool useUnscaledTime)
            {
                _remainingSeconds = remainingSeconds < 0 ? 0 : remainingSeconds;
                DurationSeconds = _remainingSeconds;
                _onTick = onTick;
                _onCompleted = onCompleted;
                Owner = owner;
                _useUnscaledTime = useUnscaledTime;
            }

            public object Owner { get; }
            public int DurationSeconds { get; }
            public int RemainingSeconds => _remainingSeconds;
            public bool IsRunning { get; private set; }
            public bool IsCompleted { get; private set; }
            public bool IsDisposed { get; private set; }

            public void Start(bool emitTickOnStart = true)
            {
                if (IsDisposed || IsCompleted || IsRunning)
                {
                    return;
                }

                if (_remainingSeconds <= 0)
                {
                    CompleteNow();
                    return;
                }

                IsRunning = true;
                _isPaused = false;

                if (emitTickOnStart)
                {
                    _onTick?.Invoke(_remainingSeconds);
                }

                ScheduleTick();
            }

            public void Pause()
            {
                if (IsDisposed || IsCompleted || !IsRunning)
                {
                    return;
                }

                CancelTick();
                IsRunning = false;
                _isPaused = true;
            }

            public void Resume(bool emitTickOnResume = true)
            {
                if (IsDisposed || IsCompleted || IsRunning || !_isPaused)
                {
                    return;
                }

                IsRunning = true;
                _isPaused = false;

                if (emitTickOnResume)
                {
                    _onTick?.Invoke(_remainingSeconds);
                }

                if (_remainingSeconds <= 0)
                {
                    CompleteNow();
                    return;
                }

                ScheduleTick();
            }

            public void Cancel()
            {
                if (IsDisposed)
                {
                    return;
                }

                CancelTick();
                IsRunning = false;
                _isPaused = false;
                DisposeInternal();
            }

            private void ScheduleTick()
            {
                CancelTick();
                _tickHandle = UniTaskHelper.Interval(TickIntervalSeconds, OnTick, Owner, _useUnscaledTime);
            }

            private void CancelTick()
            {
                if (!_tickHandle.IsEmpty && !_tickHandle.IsCancelled)
                {
                    _tickHandle.Cancel();
                }

                _tickHandle = UniTaskHelper.Handle.Empty;
            }

            private void OnTick()
            {
                if (IsDisposed || IsCompleted || !IsRunning)
                {
                    return;
                }

                _remainingSeconds--;
                if (_remainingSeconds <= 0)
                {
                    _remainingSeconds = 0;
                    CompleteNow();
                    return;
                }

                _onTick?.Invoke(_remainingSeconds);
            }

            private void CompleteNow()
            {
                CancelTick();
                IsRunning = false;
                IsCompleted = true;
                _remainingSeconds = 0;

                _onTick?.Invoke(0);
                _onCompleted?.Invoke();
                DisposeInternal();
            }

            private void DisposeInternal()
            {
                if (IsDisposed)
                {
                    return;
                }

                IsDisposed = true;
                UnbindHandle(this);
            }
        }
    }
}
