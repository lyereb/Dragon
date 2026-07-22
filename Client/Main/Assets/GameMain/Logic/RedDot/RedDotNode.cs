using System.Collections.Generic;

namespace Dragon.Logic
{
    internal sealed class RedDotNode
    {
        public string Key;
        public string ParentKey;
        public bool IsLeaf;
        public RedDotAggregateMode AggregateMode;
        public IRedDotProvider Provider;
        public readonly List<string> ChildrenKeys = new List<string>(4);
        public RedDotValue CachedValue;
    }
}
