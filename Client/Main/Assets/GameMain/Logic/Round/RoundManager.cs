using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Dragon.Library;

namespace Dragon.Logic
{
    /// <summary>
    /// 整场回合调度：一轮整场依次为
    /// 整场 Prepare（同时通知全员，等全员完成）→ 整场 Execute（各人 Turn 依次）→
    /// 整场 End（等待全员上报 RoundEnd 完成）。整场结束后同步参与者列表并可自动进入下一轮。
    /// </summary>
    public class RoundManager
    {
        #region //静态变量
        protected static RoundManager _instance = null;

        public static RoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RoundManager();
                }

                return _instance;
            }
        }
        #endregion

        private readonly List<IRoundParticipant> _participants = new List<IRoundParticipant>(16);
        private List<IRoundParticipant> _roundQueue = new List<IRoundParticipant>(16);

        /// <summary>是否处于连续整场会话中（从 StartRound 到 AbortRound 或参与者清空）。</summary>
        private bool _sessionActive;

        /// <summary>当前是否处于一整场周期内（Prepare / Execute / End 任一进行中）。</summary>
        private bool _roundCycleActive;

        private RoundPhase _roundPhase = RoundPhase.None;
        private RoundType _currentRoundType = RoundType.Explore;
        private int _participantIndex;
        private TurnPhase _currentTurnPhase = TurnPhase.None;
        private IRoundParticipant _currentTurnParticipant;
        private readonly Queue<IRoundParticipant> _pendingInsertedTurnParticipants = new Queue<IRoundParticipant>();
        private int _phaseToken;
        /// <summary>
        /// 已收到 RoundEnd 回执的参与者集合。
        /// 该回执允许在 Prepare/Execute 阶段提前到达，用于支持“表现异步播放、流程快速推进”。
        /// </summary>
        private readonly HashSet<IRoundParticipant> _roundEndReceiptParticipants = new HashSet<IRoundParticipant>();
        private readonly HashSet<IRoundParticipant> _pendingExitParticipants = new HashSet<IRoundParticipant>();
        private int _roundEndListenToken;
        private Action _beforeRoundQueueBuildHandler;
        private const float TurnPhaseTimeoutSeconds = 5f;
        private UniTaskHelper.Handle _turnPhaseTimeoutHandle = UniTaskHelper.Handle.Empty;
        private int _turnPhaseTimeoutToken;

        #region Public API（对外方法/属性）
        /// <summary>
        /// 推荐使用顺序：
        /// 1) Init
        /// 2) SetBeforeRoundQueueBuildHandler（可选）
        /// 3) TryAddParticipant（添加初始参与者）
        /// 4) StartRound
        /// 运行中：参与者退出统一调用 TryRemoveParticipant；整场结束上报调用 NotifyRoundEndCompleted。
        /// </summary>
        /// <summary>已完整结束的整场次数（每场从 Prepare 到 End 全员完成计 1）。Clear 时归零。</summary>
        public int CompletedRoundCount { get; private set; }

        /// <summary>连续整场会话是否仍在进行（含场与场之间的间隙）。</summary>
        public bool IsSessionRunning => _sessionActive;

        /// <summary>与 <see cref="IsSessionRunning"/> 相同，表示整场会话是否未结束。</summary>
        public bool IsRoundRunning => _sessionActive;

        /// <summary>当前是否处于一整场周期内（含 Prepare / Execute / End）。场间间隙为 false。</summary>
        public bool IsRoundCycleActive => _roundCycleActive;

        /// <summary>整场回合阶段；未在进行单场周期时为 None。</summary>
        public RoundPhase CurrentRoundPhase => _roundPhase;

        /// <summary>当前整场的类型（探索/战斗）；在每场开始时重新判定。</summary>
        public RoundType CurrentRoundType => _currentRoundType;

        /// <summary>当前行动者的单回合阶段；仅在整场 Execute 且轮到某人时为非 None。</summary>
        public TurnPhase CurrentTurnPhase => _currentTurnPhase;

        /// <summary>
        /// 当前行动者；仅在整场 Execute 时有值。整场 Prepare/End 为并行阶段，不指向单一参与者。
        /// </summary>
        public IRoundParticipant CurrentParticipant =>
            _roundCycleActive && _roundPhase == RoundPhase.Execute
                ? _currentTurnParticipant
                : null;

        /// <summary>是否允许增删参与者（整场周期未进行时，或会话已结束）。</summary>
        public bool CanModifyParticipants => !_roundCycleActive;

        public void Init()
        {
            Clear();
        }

        /// <summary>重置参与者、会话、计数与进行中的周期。</summary>
        public void Clear()
        {
            AbortRoundInternal();
            _participants.Clear();
            _roundQueue.Clear();
            _pendingExitParticipants.Clear();
            CompletedRoundCount = 0;
        }

        /// <summary>在允许时添加参与者（整场周期进行中不可改）。</summary>
        public bool TryAddParticipant(IRoundParticipant participant)
        {
            if (participant == null)
            {
                Debug.LogWarning("RoundManager.TryAddParticipant: participant 为 null");
                return false;
            }

            if (_roundCycleActive)
            {
                Debug.LogWarning("RoundManager: 整场周期进行中，无法添加参与者");
                return false;
            }

            if (_participants.Contains(participant))
            {
                return false;
            }

            _participants.Add(participant);
            _pendingExitParticipants.Remove(participant);
            return true;
        }

        /// <summary>
        /// 尝试移除参与者：
        /// 整场未进行时立即移除；整场进行中标记为“下一整场开始前移除”。
        /// </summary>
        public bool TryRemoveParticipant(IRoundParticipant participant)
        {
            if (participant == null)
            {
                return false;
            }

            if (!_participants.Contains(participant))
            {
                return false;
            }

            if (_roundCycleActive)
            {
                return _pendingExitParticipants.Add(participant);
            }

            _pendingExitParticipants.Remove(participant);
            return _participants.Remove(participant);
        }

        /// <summary>是否已注册该参与者。</summary>
        public bool ContainsParticipant(IRoundParticipant participant)
        {
            return participant != null && _participants.Contains(participant);
        }

        /// <summary>当前注册参与者数量。</summary>
        public int ParticipantCount => _participants.Count;

        /// <summary>
        /// 设置“每次整场开始前”的参与者同步回调（用于按业务规则增减人）。
        /// 在 StartRoundCycle 内、构建 roundQueue 之前触发。
        /// </summary>
        public void SetBeforeRoundQueueBuildHandler(Action handler)
        {
            _beforeRoundQueueBuildHandler = handler;
        }

        /// <summary>
        /// 开始连续整场会话：首场先进入整场 Prepare；每场结束后同步参与者并进入下一场 Prepare，直到无参与者或 AbortRound。
        /// </summary>
        public void StartRound()
        {
            if (_sessionActive)
            {
                Debug.LogWarning("RoundManager: 会话已在进行");
                return;
            }

            if (_participants.Count == 0)
            {
                Debug.Log("RoundManager.StartRound: 无参与者");
                return;
            }

            _sessionActive = true;
            RebuildRoundQueue();
            StartRoundCycle();
        }

        /// <summary>强制终止连续整场会话（例如切场景时调用）。</summary>
        public void AbortRound()
        {
            AbortRoundInternal();
        }

        /// <summary>
        /// 供参与者主动上报本场 RoundEnd 回执（通常在本人的表现/动画完成后调用）。
        /// 可在 Execute/End 阶段调用，重复上报会被忽略。
        /// </summary>
        public void NotifyRoundEndCompleted(IRoundParticipant participant)
        {
            OnParticipantRoundEndReceipt(participant, _roundEndListenToken);
        }

        /// <summary>
        /// 请求在当前行动者回合结束后插入指定参与者的一次完整回合（从 TurnPrepare 开始）。
        /// 仅在整场 Execute 阶段有效，重复请求会按队列顺序依次执行。
        /// </summary>
        public bool RequestInsertTurn(IRoundParticipant participant)
        {
            if (participant == null)
            {
                return false;
            }

            if (!_roundCycleActive || _roundPhase != RoundPhase.Execute)
            {
                return false;
            }

            if (!_roundQueue.Contains(participant))
            {
                return false;
            }

            _pendingInsertedTurnParticipants.Enqueue(participant);
            return true;
        }
        #endregion

        #region Lifecycle（内部生命周期）
        private void AbortRoundInternal()
        {
            CancelTurnPhaseTimeoutWatch();
            _sessionActive = false;
            _roundCycleActive = false;
            _roundPhase = RoundPhase.None;
            _participantIndex = 0;
            _currentTurnPhase = TurnPhase.None;
            _currentTurnParticipant = null;
            _phaseToken++;
            _roundEndListenToken++;
            _roundEndReceiptParticipants.Clear();
            _roundQueue.Clear();
            _pendingInsertedTurnParticipants.Clear();
        }
        #endregion

        #region Participant Sync（参与者同步与队列构建）

        private void RebuildRoundQueue()
        {
            CleanupPendingExitParticipants();
            _roundQueue.Clear();
            _roundQueue.AddRange(
                _participants.OrderBy(p => p.Faction).ThenBy(p => p.TurnOrder));
        }

        private void CleanupPendingExitParticipants()
        {
            if (_pendingExitParticipants.Count == 0)
            {
                return;
            }

            foreach (var participant in _pendingExitParticipants)
            {
                _participants.Remove(participant);
                _roundEndReceiptParticipants.Remove(participant);
            }

            _pendingExitParticipants.Clear();
            CleanupPendingInsertedTurnParticipants();
        }

        private void CleanupPendingInsertedTurnParticipants()
        {
            if (_pendingInsertedTurnParticipants.Count == 0)
            {
                return;
            }

            int count = _pendingInsertedTurnParticipants.Count;
            for (int i = 0; i < count; i++)
            {
                var participant = _pendingInsertedTurnParticipants.Dequeue();
                if (participant != null && _participants.Contains(participant))
                {
                    _pendingInsertedTurnParticipants.Enqueue(participant);
                }
            }
        }
        #endregion

        #region Round Cycle Flow（整场流程）

        private void StartRoundCycle()
        {
            if (!_sessionActive)
            {
                return;
            }

            _currentRoundType = ResolveRoundType();
            InvokeBeforeRoundQueueBuildHandler();
            RebuildRoundQueue();
            if (_roundQueue.Count == 0)
            {
                Debug.Log("RoundManager: 无参与者，结束整场回合");
                _sessionActive = false;
                _roundCycleActive = false;
                _roundPhase = RoundPhase.None;
                return;
            }

            _roundCycleActive = true;
            _roundPhase = RoundPhase.Prepare;
            _participantIndex = 0;
            _currentTurnPhase = TurnPhase.None;
            _currentTurnParticipant = null;
            _phaseToken++;
            _roundEndListenToken++;
            _roundEndReceiptParticipants.Clear();
            _pendingInsertedTurnParticipants.Clear();
            BeginRoundPrepareParallel();
        }

        private RoundType ResolveRoundType()
        {
            return RoundType.Explore;
        }

        private void InvokeBeforeRoundQueueBuildHandler()
        {
            if (_beforeRoundQueueBuildHandler == null)
            {
                return;
            }

            try
            {
                _beforeRoundQueueBuildHandler.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"RoundManager: BeforeRoundQueueBuild 回调异常: {e}");
            }
        }
        private void BeginRoundPrepareParallel()
        {
            if (!_roundCycleActive || _roundPhase != RoundPhase.Prepare)
            {
                return;
            }

            int n = _roundQueue.Count;
            if (n == 0)
            {
                EnterRoundExecutePhase();
                return;
            }

            int token = ++_phaseToken;
            int completed = 0;

            void OnOneComplete()
            {
                if (!_roundCycleActive || token != _phaseToken || _roundPhase != RoundPhase.Prepare)
                {
                    return;
                }

                completed++;
                if (completed == n)
                {
                    EnterRoundExecutePhase();
                }
            }

            foreach (var p in _roundQueue)
            {
                try
                {
                    p.OnRoundPrepare(OnOneComplete);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"RoundManager: 参与者 OnRoundPrepare 回调异常。{BuildRoundLogContext(p, RoundPhase.Prepare, token)} exception={e}");
                }
            }
        }

        private void EnterRoundExecutePhase()
        {
            if (!_roundCycleActive)
            {
                return;
            }

            _roundPhase = RoundPhase.Execute;
            _participantIndex = 0;
            _currentTurnPhase = TurnPhase.None;
            _currentTurnParticipant = null;
            BeginParticipantTurn();
        }

        private void OnParticipantRoundEndReceipt(IRoundParticipant participant, int token)
        {
            if (!_roundCycleActive || token != _roundEndListenToken)
            {
                return;
            }

            if (participant == null || !_roundQueue.Contains(participant))
            {
                return;
            }

            if (!_roundEndReceiptParticipants.Add(participant))
            {
                return;
            }

            if (_roundPhase == RoundPhase.End)
            {
                TryCompleteRoundEndPhase();
            }            
        }

        private void BeginParticipantTurn()
        {
            if (!_roundCycleActive || _roundPhase != RoundPhase.Execute)
            {
                return;
            }

            if (_participantIndex >= _roundQueue.Count)
            {
                EnterRoundEndPhase();
                return;
            }

            _currentTurnParticipant = _roundQueue[_participantIndex];
            BeginTurnPhase(TurnPhase.Prepare);
        }

        private void BeginTurnPhase(TurnPhase phase)
        {
            _currentTurnPhase = phase;
            int token = ++_phaseToken;
            var p = _currentTurnParticipant;
            if (p == null)
            {
                Debug.LogWarning($"RoundManager: 当前行动者为空，强制结束会话。{BuildTurnLogContext(null, phase, token)}");
                AbortRoundInternal();
                return;
            }
            StartTurnPhaseTimeoutWatch(p, phase, token);

            void Complete()
            {
                if (!_roundCycleActive || token != _phaseToken)
                {
                    return;
                }

                AdvanceTurnPhase();
            }

            switch (phase)
            {
                case TurnPhase.Prepare:
                    try
                    {
                        p.OnTurnPrepare(Complete);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"RoundManager: 参与者 OnTurnPrepare 回调异常。{BuildTurnLogContext(p, phase, token)} exception={e}");
                    }
                    break;
                case TurnPhase.WaitCommand:
                    try
                    {
                        p.OnTurnWaitCommand(Complete);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"RoundManager: 参与者 OnTurnWaitCommand 回调异常。{BuildTurnLogContext(p, phase, token)} exception={e}");
                    }
                    break;
                case TurnPhase.Execute:
                    try
                    {
                        p.OnTurnExecute(Complete);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"RoundManager: 参与者 OnTurnExecute 回调异常。{BuildTurnLogContext(p, phase, token)} exception={e}");
                    }
                    break;
                case TurnPhase.End:
                    try
                    {
                        p.OnTurnEnd(Complete);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"RoundManager: 参与者 OnTurnEnd 回调异常。{BuildTurnLogContext(p, phase, token)} exception={e}");
                    }
                    break;
                default:
                    Debug.LogError($"RoundManager: 未处理的单回合阶段 {phase}。{BuildTurnLogContext(p, phase, token)}");
                    Complete();
                    break;
            }
        }

        private void AdvanceTurnPhase()
        {
            switch (_currentTurnPhase)
            {
                case TurnPhase.Prepare:
                    BeginTurnPhase(TurnPhase.WaitCommand);
                    break;
                case TurnPhase.WaitCommand:
                    BeginTurnPhase(TurnPhase.Execute);
                    break;
                case TurnPhase.Execute:
                    BeginTurnPhase(TurnPhase.End);
                    break;
                case TurnPhase.End:
                    CancelTurnPhaseTimeoutWatch();
                    if (TryStartInsertedTurn())
                    {
                        break;
                    }
                    _participantIndex++;
                    BeginParticipantTurn();
                    break;
                default:
                    Debug.LogWarning("RoundManager: 异常的单回合阶段，强制结束会话");
                    AbortRoundInternal();
                    break;
            }
        }

        private void EnterRoundEndPhase()
        {
            if (!_roundCycleActive)
            {
                return;
            }

            CancelTurnPhaseTimeoutWatch();
            _roundPhase = RoundPhase.End;
            _currentTurnPhase = TurnPhase.None;
            _currentTurnParticipant = null;
            _phaseToken++;
            TryCompleteRoundEndPhase();
        }

        private void TryCompleteRoundEndPhase()
        {
            if (!_roundCycleActive || _roundPhase != RoundPhase.End)
            {
                return;
            }

            if (_roundQueue.Count == 0)
            {
                OnRoundCycleFullyCompleted();
                return;
            }

            if (_roundEndReceiptParticipants.Count >= _roundQueue.Count)
            {
                OnRoundCycleFullyCompleted();
            }
        }

        /// <summary>单场周期从 Prepare 到 End 全员完成。</summary>
        private void OnRoundCycleFullyCompleted()
        {
            CancelTurnPhaseTimeoutWatch();
            CompletedRoundCount++;
            _roundCycleActive = false;
            _roundPhase = RoundPhase.None;
            _participantIndex = 0;
            _currentTurnPhase = TurnPhase.None;
            _currentTurnParticipant = null;
            _phaseToken++;
            _roundEndReceiptParticipants.Clear();
            _pendingInsertedTurnParticipants.Clear();

            if (!_sessionActive)
            {
                return;
            }

            StartRoundCycle();
        }
        #endregion

        #region Timeout Watch（超时监控）

        private void StartTurnPhaseTimeoutWatch(IRoundParticipant participant, TurnPhase phase, int phaseTokenAtStart)
        {
            CancelTurnPhaseTimeoutWatch();
            if (participant == null)
            {
                return;
            }

            // 等待命令阶段可能是正常长耗时，不做超时告警。
            if (phase == TurnPhase.WaitCommand)
            {
                return;
            }

            _turnPhaseTimeoutToken++;
            int timeoutToken = _turnPhaseTimeoutToken;
            _turnPhaseTimeoutHandle = UniTaskHelper.Delay(TurnPhaseTimeoutSeconds, () =>
            {
                if (!_roundCycleActive || _roundPhase != RoundPhase.Execute)
                {
                    return;
                }

                if (timeoutToken != _turnPhaseTimeoutToken || phaseTokenAtStart != _phaseToken)
                {
                    return;
                }

                if (!ReferenceEquals(CurrentParticipant, participant))
                {
                    return;
                }

                Debug.LogError(
                    $"RoundManager: 角色回合阶段超时，超过 {TurnPhaseTimeoutSeconds:0.##} 秒未完成。{BuildTurnLogContext(participant, phase, phaseTokenAtStart)}");
            }, this, true);
        }

        private string BuildRoundLogContext(IRoundParticipant participant, RoundPhase phase, int token)
        {
            int round = CompletedRoundCount + (_roundCycleActive ? 1 : 0);
            return $"ctx={{round={round}, roundPhase={phase}, participant={participant}, token={token}, queueCount={_roundQueue.Count}}}";
        }

        private string BuildTurnLogContext(IRoundParticipant participant, TurnPhase phase, int token)
        {
            int round = CompletedRoundCount + (_roundCycleActive ? 1 : 0);
            return
                $"ctx={{round={round}, roundPhase={_roundPhase}, participant={participant}, participantIndex={_participantIndex}, phase={phase}, token={token}, queueCount={_roundQueue.Count}}}";
        }

        private void CancelTurnPhaseTimeoutWatch()
        {
            _turnPhaseTimeoutToken++;
            if (!_turnPhaseTimeoutHandle.IsEmpty && !_turnPhaseTimeoutHandle.IsCancelled)
            {
                _turnPhaseTimeoutHandle.Cancel();
            }
            _turnPhaseTimeoutHandle = UniTaskHelper.Handle.Empty;
        }

        private bool TryStartInsertedTurn()
        {
            while (_pendingInsertedTurnParticipants.Count > 0)
            {
                var insertedParticipant = _pendingInsertedTurnParticipants.Dequeue();
                if (insertedParticipant == null || !_roundQueue.Contains(insertedParticipant))
                {
                    continue;
                }

                if (_pendingExitParticipants.Contains(insertedParticipant))
                {
                    continue;
                }

                _currentTurnParticipant = insertedParticipant;
                BeginTurnPhase(TurnPhase.Prepare);
                return true;
            }

            return false;
        }
        #endregion
    }
}
