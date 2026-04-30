using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 秒表辅助类：基于 Scheduler 实现，支持暂停/恢复/按 owner 批量取消。
    /// </summary>
    public static class StopwatchHelper
    {
        private static readonly List<StopwatchHandle> _handles = new List<StopwatchHandle>(64);
        private static readonly Dictionary<object, List<StopwatchHandle>> _ownerHandles = new Dictionary<object, List<StopwatchHandle>>(32);

        /// <summary>
        /// 创建并启动一个秒表。
        /// </summary>
        /// <param name="onTick">每次 tick 回调，参数为已流逝整秒（向下取整）</param>
        /// <param name="owner">作用域对象（UI/State/系统模块）</param>
        /// <param name="useUnscaledTime">是否使用 unscaled 时间</param>
        /// <param name="tickIntervalSeconds">tick 间隔（秒），默认 1 秒</param>
        /// <param name="emitTickOnStart">是否创建后立即触发一次 tick</param>
        public static StopwatchHandle StartStopwatch(
            Action<int> onTick,
            object owner = null,
            bool useUnscaledTime = true,
            float tickIntervalSeconds = 1f,
            bool emitTickOnStart = true)
        {
            if (tickIntervalSeconds <= 0f)
            {
                tickIntervalSeconds = 0.1f;
            }

            var handle = new StopwatchHandle(onTick, owner, useUnscaledTime, tickIntervalSeconds);
            _handles.Add(handle);
            AddOwnerBinding(owner, handle);
            handle.Start(emitTickOnStart);
            return handle;
        }

        /// <summary>
        /// 取消某个 owner 下的全部秒表。
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

                if (!handle.IsDisposed)
                {
                    handle.Stop();
                    cancelled++;
                }
            }

            _ownerHandles.Remove(owner);
            return cancelled;
        }

        /// <summary>
        /// 取消全部秒表。
        /// </summary>
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

        private static void AddOwnerBinding(object owner, StopwatchHandle handle)
        {
            if (owner == null || handle == null)
            {
                return;
            }

            if (!_ownerHandles.TryGetValue(owner, out var list))
            {
                list = new List<StopwatchHandle>(4);
                _ownerHandles.Add(owner, list);
            }

            list.Add(handle);
        }

        private static void RemoveHandle(StopwatchHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            _handles.Remove(handle);

            object owner = handle.Owner;
            if (owner == null)
            {
                return;
            }

            if (_ownerHandles.TryGetValue(owner, out var list))
            {
                list.Remove(handle);
                if (list.Count == 0)
                {
                    _ownerHandles.Remove(owner);
                }
            }
        }

        public sealed class StopwatchHandle
        {
            private readonly Action<int> _onTick;
            private readonly bool _useUnscaledTime;
            private readonly float _tickIntervalSeconds;

            private int _tickTaskId = -1;
            private float _startTime;
            private float _accumulatedSeconds;
            private float _elapsedSecondsAtPause;
            private bool _isPaused;

            internal StopwatchHandle(Action<int> onTick, object owner, bool useUnscaledTime, float tickIntervalSeconds)
            {
                _onTick = onTick;
                Owner = owner;
                _useUnscaledTime = useUnscaledTime;
                _tickIntervalSeconds = tickIntervalSeconds;
            }

            public object Owner { get; }
            public bool IsRunning { get; private set; }
            public bool IsDisposed { get; private set; }

            public float ElapsedSeconds
            {
                get
                {
                    if (IsDisposed)
                    {
                        return _accumulatedSeconds;
                    }

                    if (IsRunning)
                    {
                        float now = GetNow(_useUnscaledTime);
                        _accumulatedSeconds = _elapsedSecondsAtPause + (now - _startTime);
                    }

                    return _accumulatedSeconds < 0f ? 0f : _accumulatedSeconds;
                }
            }

            public void Start(bool emitTickOnStart = true)
            {
                if (IsDisposed || IsRunning)
                {
                    return;
                }

                _startTime = GetNow(_useUnscaledTime);
                _elapsedSecondsAtPause = _accumulatedSeconds;
                IsRunning = true;
                _isPaused = false;

                if (emitTickOnStart)
                {
                    EmitTick();
                }

                _tickTaskId = GameCenter.SchedulerManager.Interval(_tickIntervalSeconds, OnTickInternal, this, _useUnscaledTime);
            }

            public void Pause()
            {
                if (IsDisposed || !IsRunning)
                {
                    return;
                }

                float now = GetNow(_useUnscaledTime);
                _accumulatedSeconds = _elapsedSecondsAtPause + (now - _startTime);
                _elapsedSecondsAtPause = _accumulatedSeconds;

                IsRunning = false;
                _isPaused = true;
                CancelTickTask();
            }

            public void Resume(bool emitTickOnResume = true)
            {
                if (IsDisposed || IsRunning || !_isPaused)
                {
                    return;
                }

                _startTime = GetNow(_useUnscaledTime);
                IsRunning = true;
                _isPaused = false;

                if (emitTickOnResume)
                {
                    EmitTick();
                }

                _tickTaskId = GameCenter.SchedulerManager.Interval(_tickIntervalSeconds, OnTickInternal, this, _useUnscaledTime);
            }

            public void Reset(bool emitTickAfterReset = true)
            {
                if (IsDisposed)
                {
                    return;
                }

                _accumulatedSeconds = 0f;
                _elapsedSecondsAtPause = 0f;
                _startTime = GetNow(_useUnscaledTime);

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

                if (IsRunning)
                {
                    float now = GetNow(_useUnscaledTime);
                    _accumulatedSeconds = _elapsedSecondsAtPause + (now - _startTime);
                    _elapsedSecondsAtPause = _accumulatedSeconds;
                }

                IsRunning = false;
                _isPaused = false;
                CancelTickTask();
                DisposeInternal();
            }

            private void OnTickInternal()
            {
                if (IsDisposed || !IsRunning)
                {
                    return;
                }

                EmitTick();
            }

            private void EmitTick()
            {
                float elapsed = ElapsedSeconds;
                int wholeSeconds = Mathf.FloorToInt(elapsed);
                if (wholeSeconds < 0)
                {
                    wholeSeconds = 0;
                }

                _onTick?.Invoke(wholeSeconds);
            }

            private void CancelTickTask()
            {
                if (_tickTaskId > 0)
                {
                    GameCenter.SchedulerManager.Cancel(_tickTaskId);
                    _tickTaskId = -1;
                }
            }

            private void DisposeInternal()
            {
                if (IsDisposed)
                {
                    return;
                }

                IsDisposed = true;
                StopwatchHelper.RemoveHandle(this);
            }

            private static float GetNow(bool useUnscaledTime)
            {
                return useUnscaledTime ? Time.unscaledTime : Time.time;
            }
        }
    }
}
