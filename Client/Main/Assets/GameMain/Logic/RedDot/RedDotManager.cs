using System.Collections.Generic;
using Dragon.Library;
using UnityEngine;

namespace Dragon.Logic
{
    /// <summary>
    /// 红点中枢：维护树结构、缓存数值、向上聚合、派发 UI 事件。
    /// 使用前在 GameCenter 等处调用 <see cref="Init"/>；业务注册见 <see cref="RedDotRegistry"/>。
    /// </summary>
    public sealed class RedDotManager
    {
        #region Singleton

        private static RedDotManager _instance;

        public static RedDotManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RedDotManager();
                }

                return _instance;
            }
        }

        #endregion

        private readonly Dictionary<string, RedDotNode> _nodes =
            new Dictionary<string, RedDotNode>(32);

        private readonly Dictionary<int, EventSystemHandler> _eventHandlers =
            new Dictionary<int, EventSystemHandler>(8);

        private readonly Dictionary<int, List<string>> _eventWatchKeys =
            new Dictionary<int, List<string>>(8);

        private bool _initialized;

        public void Init()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            RedDotRegistry.RegisterAll(this);
            RefreshAll();
            Debug.Log("RedDotManager Init");
        }

        public void Clear()
        {
            UnregisterWatchEvents();
            _nodes.Clear();
            _initialized = false;
            RedDotSeenStore.Instance.ClearSessionSeen();
            Debug.Log("RedDotManager Clear");
        }

        #region 树注册

        /// <summary>注册非叶子节点（只做聚合）。</summary>
        public void RegisterNode(string key, string parentKey, RedDotAggregateMode aggregateMode)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("RedDotManager.RegisterNode: key 为空");
                return;
            }

            RedDotNode node = GetOrCreateNode(key);
            node.IsLeaf = false;
            node.ParentKey = parentKey;
            node.AggregateMode = aggregateMode;
            node.Provider = null;
            LinkChild(parentKey, key);
        }

        /// <summary>注册叶子节点并绑定 Provider。</summary>
        public void RegisterLeaf(string key, string parentKey, IRedDotProvider provider)
        {
            if (string.IsNullOrEmpty(key) || provider == null)
            {
                Debug.LogError("RedDotManager.RegisterLeaf: key 或 provider 无效");
                return;
            }

            if (provider.Key != key)
            {
                Debug.LogWarning($"RedDotManager.RegisterLeaf: provider.Key({provider.Key}) 与 key({key}) 不一致，以 key 为准");
            }

            RedDotNode node = GetOrCreateNode(key);
            node.IsLeaf = true;
            node.ParentKey = parentKey;
            node.Provider = provider;
            LinkChild(parentKey, key);
            RegisterWatchEvents(provider);
        }

        #endregion

        #region 查询与刷新

        public RedDotValue GetValue(string key)
        {
            if (string.IsNullOrEmpty(key) || !_nodes.TryGetValue(key, out RedDotNode node))
            {
                return RedDotValue.None;
            }

            return node.CachedValue;
        }

        public bool IsVisible(string key) => GetValue(key).Visible;

        /// <summary>标记脏数据并重算（叶子或整棵子树）。</summary>
        public void MarkDirty(string key)
        {
            if (string.IsNullOrEmpty(key) || !_nodes.TryGetValue(key, out RedDotNode node))
            {
                return;
            }

            if (node.IsLeaf)
            {
                RedDotValue oldValue = node.CachedValue;
                RecalculateLeaf(node);
                if (node.CachedValue.Count != oldValue.Count)
                {
                    NotifyChanged(node.Key, node.CachedValue);
                }

                PropagateUp(node.ParentKey);
                return;
            }

            RefreshSubtree(key);
        }

        /// <summary>全量重算（Init 或登录后拉取数据完成时调用）。</summary>
        public void RefreshAll()
        {
            foreach (KeyValuePair<string, RedDotNode> pair in _nodes)
            {
                if (pair.Value.IsLeaf)
                {
                    RecalculateLeaf(pair.Value);
                }
            }

            foreach (KeyValuePair<string, RedDotNode> pair in _nodes)
            {
                if (!pair.Value.IsLeaf)
                {
                    RecalculateInternal(pair.Value);
                }
            }

            foreach (KeyValuePair<string, RedDotNode> pair in _nodes)
            {
                NotifyChanged(pair.Key, pair.Value.CachedValue);
            }
        }

        /// <summary>
        /// 标记已读：对 SeenUntilEnter / OncePerSession 生效，并重算该 key。
        /// </summary>
        public void MarkSeen(string key)
        {
            if (string.IsNullOrEmpty(key) || !_nodes.TryGetValue(key, out RedDotNode node) || !node.IsLeaf)
            {
                return;
            }

            if (node.Provider == null)
            {
                return;
            }

            RedDotRefreshMode mode = node.Provider.RefreshMode;
            if (mode != RedDotRefreshMode.SeenUntilEnter && mode != RedDotRefreshMode.OncePerSession)
            {
                return;
            }

            RedDotSeenStore.Instance.MarkSeen(key, mode);
            MarkDirty(key);
        }

        #endregion

        #region 内部逻辑

        private RedDotNode GetOrCreateNode(string key)
        {
            if (!_nodes.TryGetValue(key, out RedDotNode node))
            {
                node = new RedDotNode { Key = key };
                _nodes[key] = node;
            }

            return node;
        }

        private void LinkChild(string parentKey, string childKey)
        {
            if (string.IsNullOrEmpty(parentKey))
            {
                return;
            }

            RedDotNode parent = GetOrCreateNode(parentKey);
            if (!parent.ChildrenKeys.Contains(childKey))
            {
                parent.ChildrenKeys.Add(childKey);
            }
        }

        private void RegisterWatchEvents(IRedDotProvider provider)
        {
            int[] eventIds = provider.WatchEventIds;
            if (eventIds == null || eventIds.Length == 0)
            {
                return;
            }

            string providerKey = provider.Key;
            for (int i = 0; i < eventIds.Length; i++)
            {
                int eventId = eventIds[i];
                if (!_eventWatchKeys.TryGetValue(eventId, out List<string> keys))
                {
                    keys = new List<string>(2);
                    _eventWatchKeys[eventId] = keys;
                    EventSystemHandler handler = (_, __) => OnWatchEventTriggered(eventId);
                    _eventHandlers[eventId] = handler;
                    EventManager.Instance.Register(eventId, handler);
                }

                if (!keys.Contains(providerKey))
                {
                    keys.Add(providerKey);
                }
            }
        }

        private void OnWatchEventTriggered(int eventId)
        {
            if (!_eventWatchKeys.TryGetValue(eventId, out List<string> keys))
            {
                return;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                MarkDirty(keys[i]);
            }
        }

        private void UnregisterWatchEvents()
        {
            foreach (KeyValuePair<int, EventSystemHandler> pair in _eventHandlers)
            {
                EventManager.Instance.UnRegister(pair.Key, pair.Value);
            }

            _eventHandlers.Clear();
            _eventWatchKeys.Clear();
        }

        private void RefreshSubtree(string rootKey)
        {
            if (!_nodes.TryGetValue(rootKey, out RedDotNode root))
            {
                return;
            }

            RefreshNodeRecursive(root);
            NotifySubtreeChanged(rootKey);
        }

        private void RefreshNodeRecursive(RedDotNode node)
        {
            for (int i = 0; i < node.ChildrenKeys.Count; i++)
            {
                if (_nodes.TryGetValue(node.ChildrenKeys[i], out RedDotNode child))
                {
                    RefreshNodeRecursive(child);
                }
            }

            if (node.IsLeaf)
            {
                RecalculateLeaf(node);
            }
            else
            {
                RecalculateInternal(node);
            }
        }

        private void RecalculateLeaf(RedDotNode node)
        {
            if (node.Provider == null)
            {
                node.CachedValue = RedDotValue.None;
                return;
            }

            RedDotValue raw = node.Provider.Evaluate();
            RedDotRefreshMode mode = node.Provider.RefreshMode;
            if ((mode == RedDotRefreshMode.SeenUntilEnter || mode == RedDotRefreshMode.OncePerSession)
                && RedDotSeenStore.Instance.IsSeen(node.Key, mode))
            {
                node.CachedValue = RedDotValue.None;
                return;
            }

            node.CachedValue = raw;
        }

        private void RecalculateInternal(RedDotNode node)
        {
            if (node.ChildrenKeys.Count == 0)
            {
                node.CachedValue = RedDotValue.None;
                return;
            }

            switch (node.AggregateMode)
            {
                case RedDotAggregateMode.Sum:
                {
                    int sum = 0;
                    for (int i = 0; i < node.ChildrenKeys.Count; i++)
                    {
                        if (_nodes.TryGetValue(node.ChildrenKeys[i], out RedDotNode child))
                        {
                            sum += child.CachedValue.Count;
                        }
                    }

                    node.CachedValue = new RedDotValue(sum);
                    break;
                }
                case RedDotAggregateMode.Max:
                {
                    int max = 0;
                    for (int i = 0; i < node.ChildrenKeys.Count; i++)
                    {
                        if (_nodes.TryGetValue(node.ChildrenKeys[i], out RedDotNode child))
                        {
                            if (child.CachedValue.Count > max)
                            {
                                max = child.CachedValue.Count;
                            }
                        }
                    }

                    node.CachedValue = new RedDotValue(max);
                    break;
                }
                default:
                {
                    bool any = false;
                    for (int i = 0; i < node.ChildrenKeys.Count; i++)
                    {
                        if (_nodes.TryGetValue(node.ChildrenKeys[i], out RedDotNode child)
                            && child.CachedValue.Visible)
                        {
                            any = true;
                            break;
                        }
                    }

                    node.CachedValue = any ? new RedDotValue(1) : RedDotValue.None;
                    break;
                }
            }
        }

        private void PropagateUp(string parentKey)
        {
            string current = parentKey;
            while (!string.IsNullOrEmpty(current) && _nodes.TryGetValue(current, out RedDotNode node))
            {
                RedDotValue oldValue = node.CachedValue;
                RecalculateInternal(node);
                if (node.CachedValue.Count != oldValue.Count)
                {
                    NotifyChanged(node.Key, node.CachedValue);
                }

                current = node.ParentKey;
            }
        }

        private void NotifySubtreeChanged(string rootKey)
        {
            if (!_nodes.TryGetValue(rootKey, out RedDotNode root))
            {
                return;
            }

            NotifyNodeChangedRecursive(root);
        }

        private void NotifyNodeChangedRecursive(RedDotNode node)
        {
            for (int i = 0; i < node.ChildrenKeys.Count; i++)
            {
                if (_nodes.TryGetValue(node.ChildrenKeys[i], out RedDotNode child))
                {
                    NotifyNodeChangedRecursive(child);
                }
            }

            NotifyChanged(node.Key, node.CachedValue);
        }

        private void NotifyChanged(string key, RedDotValue value)
        {
            EventManager.Instance.TriggerEvent(
                UIEventDefine.EID_REDDOT_CHANGED,
                new RedDotChangedEventParam(key, value));
        }

        #endregion
    }
}
