using UnityEngine;

public static class GlobalDefine
{
    public const string MainMenuSceneName = "MainMenu";
    public const float DepthToVerticalOffset = 0.5f;
    public const float BattleGravity = 18f;
    public const float BattleRunSpeedMultiplierX = 2f;
}

public enum DamageType
{
    Physical,
    Dragon,
    Fire,
    Frost,
    Lightning,
}
