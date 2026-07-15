using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Dragon.GameUI
{
    /// <summary>
    /// 按顺序打开 UI / 弹窗：上一项关闭后再执行下一项。弹窗依赖 <see cref="PopupBaseData.closeCallback"/>；
    /// 普通 UI 依赖 <see cref="GameUIManager.CloseUI"/> 触发的 <see cref="GameUIManager.AfterUIBaseClosed"/>。
    /// </summary>
    public sealed class UIQueueManager
    {
        /// <summary>为 false 时关闭 <see cref="LogDbg"/> 输出（异常仍会走 <see cref="Debug.LogException"/>）。</summary>
        public static bool EnableDebugLog = false;

        private static UIQueueManager _instance;
        private static long _stepIdCounter;

        public static UIQueueManager Instance => _instance ??= new UIQueueManager();

        private readonly Queue<Func<UniTask>> _queue = new Queue<Func<UniTask>>();
        /// <summary>是否正在执行某一步（含 await 弹窗关闭期间）。队列非空但尚未开始下一步时为 false。</summary>
        private bool _executingStep;

        private UIQueueManager()
        {
            LogDbg("UIQueueManager 初始化，常驻泵已启动");
            PumpForever().Forget();
        }

        private static void LogDbg(string message)
        {
            if (!EnableDebugLog)
            {
                return;
            }

            Debug.Log($"[UIQueue] {message}");
        }

        public int PendingCount => _queue.Count;

        public bool IsProcessing => _executingStep || _queue.Count > 0;

        /// <summary>清空尚未执行的队列（不会取消当前正在展示的一项）。</summary>
        public void ClearPending()
        {
            int removed = _queue.Count;
            _queue.Clear();
            LogDbg($"ClearPending 已清空 {removed} 项待执行步骤");
        }

        /// <summary>
        /// 入队弹窗。会在你传入的 <paramref name="data"/> 上链式包装 <c>closeCallback</c>，
        /// 请勿在入队后复用同一 data 实例，以免回调被覆盖。
        /// </summary>
        public void EnqueuePopup<TData>(string popupName, TData data) where TData : PopupBaseData
        {
            if (string.IsNullOrEmpty(popupName) || data == null)
            {
                Debug.LogError("UIQueueManager.EnqueuePopup: popupName 或 data 无效。");
                return;
            }

            EnqueueStep("Popup", popupName, () => RunPopupAsync(popupName, data));
        }

        /// <summary>入队单层 UI，直到该界面通过 <see cref="UIBase.Close"/> 关闭。</summary>
        public void EnqueueOpenSingleUI<T, TData>(string uiName, int sortingOrder, TData data)
            where T : UIBase<TData>
            where TData : UIBaseData
        {
            if (string.IsNullOrEmpty(uiName))
            {
                Debug.LogError("UIQueueManager.EnqueueOpenSingleUI: uiName 无效。");
                return;
            }

            EnqueueStep("SingleUI", $"{uiName} order={sortingOrder}", () => RunSingleUIAsync<T, TData>(uiName, sortingOrder, data));
        }

        /// <summary>入队主界面，直到该界面通过 <see cref="UIBase.Close"/> 关闭。</summary>
        public void EnqueueOpenMainUI<T, TData>(string uiName, TData data)
            where T : UIBase<TData>
            where TData : UIBaseData
        {
            if (string.IsNullOrEmpty(uiName))
            {
                Debug.LogError("UIQueueManager.EnqueueOpenMainUI: uiName 无效。");
                return;
            }

            EnqueueStep("MainUI", uiName, () => RunMainUIAsync<T, TData>(uiName, data));
        }

        /// <summary>
        /// 自定义一步：在需要结束时调用传入的 <paramref name="complete"/>。
        /// 若打开失败，请务必仍调用 <paramref name="complete"/>，否则队列会卡住。
        /// </summary>
        public void EnqueueCustom(Action<Action> run)
        {
            if (run == null)
            {
                Debug.LogError("UIQueueManager.EnqueueCustom: run 为空。");
                return;
            }

            EnqueueStep("Custom", "Action<Action>", () => RunCustomAsync(run));
        }

        /// <summary>异步自定义一步（例如串联 UniTask 流程）。</summary>
        public void EnqueueAsync(Func<UniTask> taskFactory)
        {
            if (taskFactory == null)
            {
                Debug.LogError("UIQueueManager.EnqueueAsync: taskFactory 为空。");
                return;
            }

            EnqueueStep("Async", "Func<UniTask>", async () => await taskFactory());
        }

        private void EnqueueStep(string kind, string detail, Func<UniTask> body)
        {
            long id = Interlocked.Increment(ref _stepIdCounter);
            _queue.Enqueue(async () =>
            {
                int stillQueued = _queue.Count;
                LogDbg($"Step begin #{id} {kind} ({detail})  | 排队中剩余={stillQueued} executingStep={_executingStep}");
                try
                {
                    await body();
                }
                finally
                {
                    LogDbg($"Step end   #{id} {kind} ({detail})");
                }
            });
            LogDbg($"Enqueued #{id} {kind} ({detail}) | pending={_queue.Count}");
        }

        /// <summary>
        /// 常驻泵：入队即由循环消费，不再依赖「收尾时再 TryStart」，避免与 UniTask 续跑时机叠加后出现漏跑。
        /// </summary>
        private async UniTaskVoid PumpForever()
        {
            while (true)
            {
                if (_queue.Count == 0)
                {
                    LogDbg("队列已空，泵进入等待（WaitUntil）…");
                    await UniTask.WaitUntil(() => _queue.Count > 0);
                    LogDbg($"泵被唤醒，即将执行，pending={_queue.Count}");
                }

                var op = _queue.Dequeue();
                _executingStep = true;
                try
                {
                    await op();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                finally
                {
                    _executingStep = false;
                }
            }
        }

        private static async UniTask RunPopupAsync<TData>(string popupName, TData data) where TData : PopupBaseData
        {
            var tcs = new UniTaskCompletionSource();
            var userClose = data.closeCallback;
            data.closeCallback = () =>
            {
                try
                {
                    userClose?.Invoke();
                }
                finally
                {
                    tcs.TrySetResult();
                }
            };

            PopupManager.Instance.OpenPopup(popupName, data, p =>
            {
                if (p == null)
                {
                    LogDbg($"RunPopupAsync OpenPopup 回调失败或未加载: {popupName}");
                    tcs.TrySetResult();
                }
                else
                {
                    LogDbg($"RunPopupAsync 已打开: {popupName} → 等待 closeCallback");
                }
            });

            await tcs.Task;
        }

        private static async UniTask RunSingleUIAsync<T, TData>(string uiName, int sortingOrder, TData data)
            where T : UIBase<TData>
            where TData : UIBaseData
        {
            await GameUIManager.Instance.OpenSingleUI<T, TData>(uiName, sortingOrder, data);

            if (!GameUIManager.Instance.TryGetCachedSingleUI(uiName, out var ui) || ui == null)
            {
                LogDbg($"RunSingleUIAsync 打开后未找到缓存: {uiName}");
                return;
            }

            await WaitForUIBaseClosedAsync(ui);
        }

        private static async UniTask RunMainUIAsync<T, TData>(string uiName, TData data)
            where T : UIBase<TData>
            where TData : UIBaseData
        {
            await GameUIManager.Instance.OpenMainUI<T, TData>(uiName, data);

            if (!GameUIManager.Instance.TryGetCachedMainUI(uiName, out var ui) || ui == null)
            {
                LogDbg($"RunMainUIAsync 打开后未找到缓存: {uiName}");
                return;
            }

            await WaitForUIBaseClosedAsync(ui);
        }

        private static async UniTask WaitForUIBaseClosedAsync(UIBase ui)
        {
            var tcs = new UniTaskCompletionSource();

            void Handler(UIBase closed)
            {
                if (closed != ui)
                {
                    return;
                }

                GameUIManager.Instance.AfterUIBaseClosed -= Handler;
                LogDbg($"WaitForUIBaseClosed: {ui.name} AfterUIBaseClosed 匹配，步骤可继续");
                tcs.TrySetResult();
            }

            GameUIManager.Instance.AfterUIBaseClosed += Handler;

            if (ui == null || !ui.gameObject.activeInHierarchy)
            {
                GameUIManager.Instance.AfterUIBaseClosed -= Handler;
                LogDbg($"WaitForUIBaseClosed: 跳过等待（ui 无效或未激活） {ui?.name}");
                return;
            }

            await tcs.Task;
        }

        private static async UniTask RunCustomAsync(Action<Action> run)
        {
            var tcs = new UniTaskCompletionSource();
            var finished = false;

            void Complete()
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                tcs.TrySetResult();
            }

            try
            {
                run(Complete);
            }
            catch (Exception ex)
            {
                Debug.LogError($"UIQueueManager 自定义步骤异常: {ex}");
                Complete();
            }

            await tcs.Task;
        }
    }
}
