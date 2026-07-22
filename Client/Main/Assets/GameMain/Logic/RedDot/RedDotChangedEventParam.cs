namespace Dragon.Logic
{
    public sealed class RedDotChangedEventParam
    {
        public string Key;
        public RedDotValue Value;

        public RedDotChangedEventParam(string key, RedDotValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
