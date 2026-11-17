using UnityEngine;

public static class GlobalDefine
{
    public const string MainMenuSceneName = "MainMenu";
    /// <summary>旧 2.5D 斜投影系数。俯视后地面不再使用。</summary>
    public const float DepthToVerticalOffset = 0.5f;
    /// <summary>跳跃重力。</summary>
    public const float BattleGravity = 18f;
    /// <summary>每 1 单位逻辑高度，身体 Body 在画面上抬高多少。阴影留在地面。</summary>
    public const float JumpVisualLift = 1f;
    public const float BattleRunSpeedMultiplierX = 2f;

    /// <summary>BGM 默认主音量（0~1）。</summary>
    public const float DefaultBgmVolume = 1f;
    /// <summary>环境音默认主音量（0~1）。</summary>
    public const float DefaultAmbientVolume = 1f;
    /// <summary>音效默认主音量（0~1）。</summary>
    public const float DefaultSfxVolume = 1f;
}
