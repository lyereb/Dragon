using cfg.dragon;
public class MapData
{
    public map RawConfig { get; private set; }

    public int MapId { get; private set; }
    public string SceneName { get; private set; }
    public float MapUpperLimit { get; private set; }
    public float MapLowerLimit { get; private set; }

    public void Initialize(map mapConfig)
    {
        RawConfig = mapConfig;
        MapId = mapConfig.Id;
        SceneName = mapConfig.SceneName;
        MapUpperLimit = mapConfig.UpperLimit;
        MapLowerLimit = mapConfig.LowerLimit;
    }
}