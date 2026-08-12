using cfg;
using cfg.dragon;

public class MapData
{
    public map RawConfig { get; private set; }

    public int MapId { get; private set; }
    public string SceneName { get; private set; }
    /// <summary>X 轴边界，X=下限，Y=上限。</summary>
    public vector2 MapBoundX { get; private set; }
    /// <summary>Y 轴边界，X=下限，Y=上限。</summary>
    public vector2 MapBoundY { get; private set; }

    public void Initialize(map mapConfig)
    {
        RawConfig = mapConfig;
        MapId = mapConfig.Id;
        SceneName = mapConfig.SceneName;
        MapBoundX = mapConfig.MapBoundX;
        MapBoundY = mapConfig.MapBoundY;
    }
}
