namespace Dragon.Logic
{
    /// <summary>整场回合的类型，用于决定回合推进规则。</summary>
    public enum RoundType
    {
        /// <summary>探索类回合：行动逻辑可先推进，动作表现可异步收尾。</summary>
        Explore = 0,
        /// <summary>战斗类回合：当前行动者完整结束后再推进到下一位。</summary>
        Combat = 1,
    }

    /// <summary>整场回合的阶段（与单个参与者的 <see cref="TurnPhase"/> 不同）。</summary>
    public enum RoundPhase
    {
        None = 0,
        /// <summary>整场准备：按顺序通知所有参与者。</summary>
        Prepare = 1,
        /// <summary>整场执行：各参与者依次进入自身回合流程。</summary>
        Execute = 2,
        /// <summary>整场结束：按顺序通知所有参与者。</summary>
        End = 3,
    }

    /// <summary>回合参与者阵营。</summary>
    public enum RoundParticipantFaction
    {
        /// <summary>我方</summary>
        Ally = 0,
        /// <summary>敌方</summary>
        Enemy = 1,
        /// <summary>中立</summary>
        Neutral = 2,
    }

    /// <summary>单个参与者一回合内的阶段。</summary>
    public enum TurnPhase
    {
        None = 0,
        /// <summary>回合准备</summary>
        Prepare = 1,
        /// <summary>等待指令</summary>
        WaitCommand = 2,
        /// <summary>执行</summary>
        Execute = 3,
        /// <summary>回合结束</summary>
        End = 4,
    }
}
