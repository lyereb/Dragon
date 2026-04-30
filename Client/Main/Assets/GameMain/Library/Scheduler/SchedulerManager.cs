using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dragon.Library
{
    public enum SchedulerTaskType
    {
        Delay = 0,
        Interval = 1,
        NextFrame = 2,
        EveryFrame = 3,
    }

    public sealed class SchedulerManager
    {
        #region //静态变量
        private static SchedulerManager _instance;
        public static SchedulerManager Instance => _instance ??= new SchedulerManager();
        #endregion

        private sealed class SchedulerTask
        {
            public int Id;
            public SchedulerTaskType Type;
            public Action Callback;
            public Action<float> DeltaCallback;
            public object Owner;
            public float Interval;
            public float Remaining;
            public bool UseUnscaledTime;
            public int Priority;
            public int CreateFrame;
            public bool Cancelled;
            public long Sequence;
        }

        private readonly Dictionary<int, SchedulerTask> _taskMap = new Dictionary<int, SchedulerTask>(256);
        private readonly List<SchedulerTask> _tasks = new List<SchedulerTask>(256);
        private readonly Stack<SchedulerTask> _taskPool = new Stack<SchedulerTask>(64);
        private readonly List<SchedulerTask> _dueTasks = new List<SchedulerTask>(64);
        private readonly List<SchedulerTask> _removeTasks = new List<SchedulerTask>(64);
        private readonly Comparison<SchedulerTask> _dueSortComparer;

        private int _nextTaskId = 1;
        private long _nextSequence = 1;
        private bool _isInitialized;

        private SchedulerManager()
        {
            _dueSortComparer = SortDueTask;
        }

        public void Init()
        {
            _isInitialized = true;
        }

        public void Shutdown()
        {
            ClearAll();
            _isInitialized = false;
        }

        public int Delay(float seconds, Action callback, object owner = null, bool useUnscaledTime = false, int priority = 0)
        {
            if (callback == null)
            {
                Debug.LogWarning("[Scheduler] Delay callback is null.");
                return -1;
            }

            if (seconds < 0f)
            {
                seconds = 0f;
            }

            var task = AcquireTask();
            SetupTask(task, SchedulerTaskType.Delay, callback, null, owner, seconds, seconds, useUnscaledTime, priority);
            RegisterTask(task);
            return task.Id;
        }

        public int Interval(float seconds, Action callback, object owner = null, bool useUnscaledTime = false, int priority = 0)
        {
            if (callback == null)
            {
                Debug.LogWarning("[Scheduler] Interval callback is null.");
                return -1;
            }

            if (seconds <= 0f)
            {
                seconds = 0.001f;
            }

            var task = AcquireTask();
            SetupTask(task, SchedulerTaskType.Interval, callback, null, owner, seconds, seconds, useUnscaledTime, priority);
            RegisterTask(task);
            return task.Id;
        }

        public int NextFrame(Action callback, object owner = null, int priority = 0)
        {
            if (callback == null)
            {
                Debug.LogWarning("[Scheduler] NextFrame callback is null.");
                return -1;
            }

            var task = AcquireTask();
            SetupTask(task, SchedulerTaskType.NextFrame, callback, null, owner, 0f, 0f, false, priority);
            RegisterTask(task);
            return task.Id;
        }

        public int EveryFrame(Action callback, object owner = null, bool useUnscaledTime = false, int priority = 0)
        {
            if (callback == null)
            {
                Debug.LogWarning("[Scheduler] EveryFrame callback is null.");
                return -1;
            }

            var task = AcquireTask();
            SetupTask(task, SchedulerTaskType.EveryFrame, callback, null, owner, 0f, 0f, useUnscaledTime, priority);
            RegisterTask(task);
            return task.Id;
        }

        public int EveryFrame(Action<float> callback, object owner = null, bool useUnscaledTime = false, int priority = 0)
        {
            if (callback == null)
            {
                Debug.LogWarning("[Scheduler] EveryFrame delta callback is null.");
                return -1;
            }

            var task = AcquireTask();
            SetupTask(task, SchedulerTaskType.EveryFrame, null, callback, owner, 0f, 0f, useUnscaledTime, priority);
            RegisterTask(task);
            return task.Id;
        }

        public bool Cancel(int taskId)
        {
            if (taskId <= 0)
            {
                return false;
            }

            if (_taskMap.TryGetValue(taskId, out var task))
            {
                task.Cancelled = true;
                return true;
            }

            return false;
        }

        public int CancelByOwner(object owner)
        {
            if (owner == null)
            {
                return 0;
            }

            int cancelledCount = 0;
            for (int i = 0; i < _tasks.Count; i++)
            {
                var task = _tasks[i];
                if (task == null || task.Cancelled)
                {
                    continue;
                }

                if (ReferenceEquals(task.Owner, owner))
                {
                    task.Cancelled = true;
                    cancelledCount++;
                }
            }

            return cancelledCount;
        }

        public void ClearAll()
        {
            for (int i = 0; i < _tasks.Count; i++)
            {
                var task = _tasks[i];
                if (task == null)
                {
                    continue;
                }

                task.Cancelled = true;
                RecycleTask(task);
            }

            _tasks.Clear();
            _taskMap.Clear();
            _dueTasks.Clear();
            _removeTasks.Clear();
        }

        public void Update(float deltaTime)
        {
            if (!_isInitialized || _tasks.Count == 0)
            {
                return;
            }

            float unscaledDeltaTime = Time.unscaledDeltaTime;
            int frame = Time.frameCount;

            _dueTasks.Clear();
            _removeTasks.Clear();

            for (int i = 0; i < _tasks.Count; i++)
            {
                var task = _tasks[i];
                if (task == null || task.Cancelled)
                {
                    if (task != null)
                    {
                        _removeTasks.Add(task);
                    }
                    continue;
                }

                if (IsOwnerInvalid(task.Owner))
                {
                    task.Cancelled = true;
                    _removeTasks.Add(task);
                    continue;
                }

                float dt = task.UseUnscaledTime ? unscaledDeltaTime : deltaTime;
                if (ShouldRunTask(task, dt, frame))
                {
                    _dueTasks.Add(task);
                }
            }

            if (_dueTasks.Count > 1)
            {
                _dueTasks.Sort(_dueSortComparer);
            }

            for (int i = 0; i < _dueTasks.Count; i++)
            {
                ExecuteTask(_dueTasks[i]);
            }

            for (int i = 0; i < _tasks.Count; i++)
            {
                var task = _tasks[i];
                if (task != null && task.Cancelled)
                {
                    _removeTasks.Add(task);
                }
            }

            if (_removeTasks.Count == 0)
            {
                return;
            }

            for (int i = _removeTasks.Count - 1; i >= 0; i--)
            {
                var task = _removeTasks[i];
                _taskMap.Remove(task.Id);
                _tasks.Remove(task);
                RecycleTask(task);
            }
        }

        private static int SortDueTask(SchedulerTask x, SchedulerTask y)
        {
            int cmp = y.Priority.CompareTo(x.Priority);
            if (cmp != 0)
            {
                return cmp;
            }

            return x.Sequence.CompareTo(y.Sequence);
        }

        private static bool IsOwnerInvalid(object owner)
        {
            if (owner == null)
            {
                return false;
            }

            if (owner is UnityEngine.Object unityObject)
            {
                return unityObject == null;
            }

            return false;
        }

        private static bool ShouldRunTask(SchedulerTask task, float dt, int frame)
        {
            switch (task.Type)
            {
                case SchedulerTaskType.NextFrame:
                    return frame > task.CreateFrame;
                case SchedulerTaskType.EveryFrame:
                    return true;
                case SchedulerTaskType.Delay:
                case SchedulerTaskType.Interval:
                    task.Remaining -= dt;
                    return task.Remaining <= 0f;
                default:
                    return false;
            }
        }

        private void ExecuteTask(SchedulerTask task)
        {
            if (task.Cancelled)
            {
                return;
            }

            float callbackDt = task.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            try
            {
                task.Callback?.Invoke();
                task.DeltaCallback?.Invoke(callbackDt);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scheduler] Task execute exception, id:{task.Id}, type:{task.Type}, owner:{task.Owner}, msg:{e}");
                task.Cancelled = true;
                return;
            }

            switch (task.Type)
            {
                case SchedulerTaskType.Delay:
                case SchedulerTaskType.NextFrame:
                    task.Cancelled = true;
                    break;
                case SchedulerTaskType.Interval:
                    task.Remaining += task.Interval;
                    break;
            }
        }

        private SchedulerTask AcquireTask()
        {
            if (_taskPool.Count > 0)
            {
                return _taskPool.Pop();
            }

            return new SchedulerTask();
        }

        private void SetupTask(
            SchedulerTask task,
            SchedulerTaskType type,
            Action callback,
            Action<float> deltaCallback,
            object owner,
            float interval,
            float remaining,
            bool useUnscaledTime,
            int priority)
        {
            task.Id = _nextTaskId++;
            task.Type = type;
            task.Callback = callback;
            task.DeltaCallback = deltaCallback;
            task.Owner = owner;
            task.Interval = interval;
            task.Remaining = remaining;
            task.UseUnscaledTime = useUnscaledTime;
            task.Priority = priority;
            task.CreateFrame = Time.frameCount;
            task.Cancelled = false;
            task.Sequence = _nextSequence++;
        }

        private void RegisterTask(SchedulerTask task)
        {
            _taskMap[task.Id] = task;
            _tasks.Add(task);
        }

        private void RecycleTask(SchedulerTask task)
        {
            task.Id = 0;
            task.Type = SchedulerTaskType.Delay;
            task.Callback = null;
            task.DeltaCallback = null;
            task.Owner = null;
            task.Interval = 0f;
            task.Remaining = 0f;
            task.UseUnscaledTime = false;
            task.Priority = 0;
            task.CreateFrame = 0;
            task.Cancelled = false;
            task.Sequence = 0;
            _taskPool.Push(task);
        }
    }
}
