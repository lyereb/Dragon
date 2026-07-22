using UnityEngine;

namespace Dragon.Logic
{
    /// <summary>
    /// 「点进即消」红点的已读持久化（PlayerPrefs）。
    /// </summary>
    public sealed class RedDotSeenStore
    {
        private const string PrefsKeyPrefix = "reddot_seen_";

        #region Singleton

        private static RedDotSeenStore _instance;

        public static RedDotSeenStore Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RedDotSeenStore();
                }

                return _instance;
            }
        }

        #endregion

        private readonly System.Collections.Generic.HashSet<string> _sessionSeenKeys =
            new System.Collections.Generic.HashSet<string>();

        public bool IsSeen(string key, RedDotRefreshMode mode)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            switch (mode)
            {
                case RedDotRefreshMode.SeenUntilEnter:
                    return PlayerPrefs.GetInt(BuildPrefsKey(key), 0) == 1;
                case RedDotRefreshMode.OncePerSession:
                    return _sessionSeenKeys.Contains(key);
                default:
                    return false;
            }
        }

        public void MarkSeen(string key, RedDotRefreshMode mode)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            switch (mode)
            {
                case RedDotRefreshMode.SeenUntilEnter:
                    PlayerPrefs.SetInt(BuildPrefsKey(key), 1);
                    PlayerPrefs.Save();
                    break;
                case RedDotRefreshMode.OncePerSession:
                    _sessionSeenKeys.Add(key);
                    break;
            }
        }

        public void ClearSeen(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            PlayerPrefs.DeleteKey(BuildPrefsKey(key));
            _sessionSeenKeys.Remove(key);
        }

        public void ClearSessionSeen()
        {
            _sessionSeenKeys.Clear();
        }

        private static string BuildPrefsKey(string key) => PrefsKeyPrefix + key;
    }
}
