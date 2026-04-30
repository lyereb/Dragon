using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 倒计时辅助类：基于 Scheduler 实现，支持暂停/恢复/按 owner 批量取消。
    /// </summary>
    public static class CountdownHelper
    {
        private static readonly List<CountdownHandle> _handles = new List<CountdownHandle>(64);
        private static readonly Dictionary<object, List<CountdownHandle>> _ownerHandles = new Dictionary<object, List<CountdownHandle>>(32);

        /// <summary>
        /// 创建并启动一个倒计时。
        /// </summary>
        /// <param name="durationSeconds">总时长（秒）</param>
        /// <param name="onTick">每次 tick 回调，参数为剩余整秒（向上取整）</param>
        /// <param name="onCompleted">倒计时结束回调</param>
        /// <param name="owner">作用域对象（UI/State/系统模块）</param>
        /// <param name="useUnscaledTime">是否使用 unscaled 时间</param>
        /// <param name="tickIntervalSeconds">tick 间隔（秒），默认 1 秒</param>
        /// <param name="emitTickOnStart">是否创建后立即触发一次 tick</param>
        public static CountdownHandle StartCountdown(
            float durationSeconds,
            Action<int> onTick,
            Action onCompleted = null,
            object owner = null,
            bool useUnscaledTime = true,
            float tickIntervalSeconds = 1f,
            bool emitTickOnStart = true)
        {
            if (durationSeconds < 0f)
            {
                durationSeconds = 0f;
            }

            if (tickIntervalSeconds <= 0f)
            {
                tickIntervalSeconds = 0.1f;
            }

            var handle = new CountdownHandle(
                durationSeconds,
                onTick,
                onCompleted,
                owner,
                useUnscaledTime,
                tickIntervalSeconds);

            _handles.Add(handle);
            AddOwnerBinding(owner, handle);
            handle.Start(emitTickOnStart);
            return handle;
        }

        /// <summary>
        /// 取消某个 owner 下的全部倒计时。
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
                    handle.Cancel();
                    cancelled++;
                }
            }

            _ownerHandles.Remove(owner);
            return cancelled;
        }

        /// <summary>
        /// 取消全部倒计时。
        /// </summary>
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

        private static void AddOwnerBinding(object owner, CountdownHandle handle)
        {
            if (owner == null || handle == null)
            {
                return;
            }

            if (!_ownerHandles.TryGetValue(owner, out var list))
            {
                list = new List<CountdownHandle>(4);
                _ownerHandles.Add(owner, list);
            }

            list.Add(handle);
        }

        private static void RemoveHandle(CountdownHandle handle)
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

        public sealed class CountdownHandle
        {
            private readonly Action<int> _onTick;
            private readonly Action _onCompleted;
            private readonly bool _useUnscaledTime;
            private readonly float _tickIntervalSeconds;

            private int _completeTaskId = -1;
            private int _tickTaskId = -1;
            private float _endTime;
            private float _remainingSeconds;
            private bool _isPaused;

            internal CountdownHandle(
                float durationSeconds,
                Action<int> onTick,
                Action onCompleted,
                object owner,
                bool useUnscaledTime,
                float tickIntervalSeconds)
            {
                DurationSeconds = durationSeconds;
                _remainingSeconds = durationSeconds;
                _onTick = onTick;
                _onCompleted = onCompleted;
                Owner = owner;
                _useUnscaledTime = useUnscaledTime;
                _tickIntervalSeconds = tickIntervalSeconds;
            }

            public object Owner { get; }
            public float DurationSeconds { get; }
            public bool IsRunning { get; private set; }
            public bool IsCompleted { get; private set; }
            public bool IsDisposed { get; private set; }

            public float RemainingSeconds
            {
                get
                {
                    if (IsDisposed)
                    {
                        return 0f;
                    }

                    if (IsRunning && !IsCompleted)
                    {
                        float now = GetNow(_useUnscaledTime);
                        float left = _endTime - now;
                        _remainingSeconds = left > 0f ? left : 0f;
                    }

                    return _remainingSeconds;
                }
            }

            public void Start(bool emitTickOnStart = true)
            {
                if (IsDisposed || IsCompleted || IsRunning)
                {
                    return;
                }

                if (DurationSeconds <= 0f)
                {
                    if (emitTickOnStart)
                    {
                        _onTick?.Invoke(0);
                    }
                    CompleteNow();
                    return;
                }

                float now = GetNow(_useUnscaledTime);
                _endTime = now + _remainingSeconds;
                IsRunning = true;
                _isPaused = false;

                if (emitTickOnStart)
                {
                    EmitTick();
                }

                ScheduleTasks();
            }

            public void Pause()
            {
                if (IsDisposed || IsCompleted || !IsRunning)
                {
                    return;
                }

                float now = GetNow(_useUnscaledTime);
                float left = _endTime - now;
                _remainingSeconds = left > 0f ? left : 0f;

                CancelScheduledTasks();
                IsRunning = false;
                _isPaused = true;
            }

            public void Resume(bool emitTickOnResume = true)
            {
                if (IsDisposed || IsCompleted || IsRunning || !_isPaused)
                {
                    return;
                }

                float now = GetNow(_useUnscaledTime);
                _endTime = now + _remainingSeconds;
                IsRunning = true;
                _isPaused = false;

                if (emitTickOnResume)
                {
                    EmitTick();
                }

                ScheduleTasks();
            }

            public void Cancel()
            {
                if (IsDisposed)
                {
                    return;
                }

                CancelScheduledTasks();
                IsRunning = false;
                _isPaused = false;
                DisposeInternal();
            }

            private void ScheduleTasks()
            {
                _completeTaskId = GameCenter.SchedulerManager.Delay(_remainingSeconds, OnCompletedInternal, this, _useUnscaledTime);
                _tickTaskId = GameCenter.SchedulerManager.Interval(_tickIntervalSeconds, OnTickInternal, this, _useUnscaledTime);
            }

            private void CancelScheduledTasks()
            {
                if (_completeTaskId > 0)
                {
                    GameCenter.SchedulerManager.Cancel(_completeTaskId);
                    _completeTaskId = -1;
                }

                if (_tickTaskId > 0)
                {
                    GameCenter.SchedulerManager.Cancel(_tickTaskId);
                    _tickTaskId = -1;
                }
            }

            private void OnTickInternal()
            {
                if (IsDisposed || IsCompleted || !IsRunning)
                {
                    return;
                }

                EmitTick();
            }

            private void EmitTick()
            {
                float now = GetNow(_useUnscaledTime);
                float left = _endTime - now;
                _remainingSeconds = left > 0f ? left : 0f;

                int wholeSeconds = Mathf.CeilToInt(_remainingSeconds);
                if (wholeSeconds < 0)
                {
                    wholeSeconds = 0;
                }

                _onTick?.Invoke(wholeSeconds);
            }

            private void OnCompletedInternal()
            {
                if (IsDisposed || IsCompleted)
                {
                    return;
                }

                CompleteNow();
            }

            private void CompleteNow()
            {
                CancelScheduledTasks();
                IsRunning = false;
                IsCompleted = true;
                _remainingSeconds = 0f;

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
                CountdownHelper.RemoveHandle(this);
            }

            private static float GetNow(bool useUnscaledTime)
            {
                return useUnscaledTime ? Time.unscaledTime : Time.time;
            }
        }
    }
}
