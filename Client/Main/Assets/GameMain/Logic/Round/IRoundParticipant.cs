using System;

namespace Dragon.Logic
{
    /// <summary>
    /// 整场回合中的参与者；各回调在逻辑结束时必须调用一次 <paramref name="onComplete"/>。
    /// </summary>
    public interface IRoundParticipant
    {
        RoundParticipantFaction Faction { get; }

        /// <summary>同阵营内出手顺序，数值越小越靠前。</summary>
        int TurnOrder { get; }

        /// <summary>整场准备阶段：与其他参与者同时收到通知；完成后调用一次 onComplete。</summary>
        void OnRoundPrepare(Action onComplete);

        void OnTurnPrepare(Action onComplete);
        void OnTurnWaitCommand(Action onComplete);
        void OnTurnExecute(Action onComplete);
        void OnTurnEnd(Action onComplete);
    }
}
