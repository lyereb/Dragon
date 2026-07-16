using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 秒表辅助类：基于 UniTaskHelper，固定 1 秒 Interval 上报已流逝整秒。
    /// </summary>
    public static class StopwatchHelper
    {
        private const float TickIntervalSeconds = 1f;

        private static readonly List<StopwatchHandle> _handles = new List<StopwatchHandle>(64);
        private static readonly Dictionary<object, List<StopwatchHandle>> _ownerHandles = new Dictionary<object, List<StopwatchHandle>>(32);

        /// <summary>
        /// 创建并启动一个秒表。
        /// </summary>
        /// <param name="onTick">每次 tick 回调，参数为已流逝整秒</param>
        /// <param name="owner">作用域对象（UI/State/系统模块）</param>
        /// <param name="useUnscaledTime">是否使用 unscaled 时间</param>
        /// <param name="emitTickOnStart">是否创建后立即触发一次 tick</param>
        public static StopwatchHandle StartStopwatch(
            Action<int> onTick,
            object owner = null,
            bool useUnscaledTime = true,
            bool emitTickOnStart = true)
        {
            var handle = new StopwatchHandle(onTick, owner, useUnscaledTime);
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
                    handle.Stop();
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
                    handle.Stop();
                }
            }

            _handles.Clear();
            _ownerHandles.Clear();
        }

        private static void BindOwner(object owner, StopwatchHandle handle)
        {
            if (owner == null || handle == null)
            {
                return;
            }

            if (!_ownerHandles.TryGetValue(owner, out var list))
            {
                list = new List<StopwatchHandle>(4);
                _ownerHandles[owner] = list;
            }

            list.Add(handle);
        }

        private static void UnbindHandle(StopwatchHandle handle)
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

        public sealed class StopwatchHandle
        {
            private readonly Action<int> _onTick;
            private readonly bool _useUnscaledTime;

            private UniTaskHelper.Handle _tickHandle = UniTaskHelper.Handle.Empty;
            private int _elapsedSeconds;
            private bool _isPaused;

            internal StopwatchHandle(Action<int> onTick, object owner, bool useUnscaledTime)
            {
                _onTick = onTick;
                Owner = owner;
                _useUnscaledTime = useUnscaledTime;
            }

            public object Owner { get; }
            public int ElapsedSeconds => _elapsedSeconds;
            public bool IsRunning { get; private set; }
            public bool IsDisposed { get; private set; }

            public void Start(bool emitTickOnStart = true)
            {
                if (IsDisposed || IsRunning)
                {
                    return;
                }

                IsRunning = true;
                _isPaused = false;

                if (emitTickOnStart)
                {
                    EmitTick();
                }

                ScheduleTick();
            }

            public void Pause()
            {
                if (IsDisposed || !IsRunning)
                {
                    return;
                }

                CancelTick();
                IsRunning = false;
                _isPaused = true;
            }

            public void Resume(bool emitTickOnResume = true)
            {
                if (IsDisposed || IsRunning || !_isPaused)
                {
                    return;
                }

                IsRunning = true;
                _isPaused = false;

                if (emitTickOnResume)
                {
                    EmitTick();
                }

                ScheduleTick();
            }

            public void Reset(bool emitTickAfterReset = true)
            {
                if (IsDisposed)
                {
                    return;
                }

                _elapsedSeconds = 0;

                if (emitTickAfterReset)
                {
                    EmitTick();
                }
            }

            public void Stop()
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
                if (IsDisposed || !IsRunning)
                {
                    return;
                }

                _elapsedSeconds++;
                EmitTick();
            }

            private void EmitTick()
            {
                _onTick?.Invoke(_elapsedSeconds < 0 ? 0 : _elapsedSeconds);
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
