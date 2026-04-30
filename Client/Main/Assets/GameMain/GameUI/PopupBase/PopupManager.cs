using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Dragon.Resource;

namespace Dragon.GameUI
{
    public class PopupManager
    {
        #region //静态变量
        //静态变量
        protected static PopupManager _instance = null;
        protected static PopupRoot _popupRoot = null;

        //静态属性
        public static PopupManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PopupManager();
                    _popupRoot = PopupRoot.Create();
                    _popupRoot.Init();
                }
                return _instance;
            }
        }
        #endregion

        private readonly Dictionary<string, PopupBase> _popupCache = new Dictionary<string, PopupBase>();

        private void BringPopupToTop(PopupBase popup)
        {
            if (popup == null)
            {
                return;
            }

            popup.transform.SetAsLastSibling();
        }

        public void OpenPopup<TData>(string popupName, TData data, System.Action<PopupBase> onOpened = null) where TData : PopupBaseData
        {
            if (_popupCache.TryGetValue(popupName, out PopupBase cachedPopup))
            {
                if (cachedPopup != null)
                {
                    BringPopupToTop(cachedPopup);
                    onOpened?.Invoke(cachedPopup);
                    return;
                }

                _popupCache.Remove(popupName);
            }

            ResourceManager.Instance.LoadAsync<GameObject>(popupName, (popupPrefab) =>
            {
                if (popupPrefab == null)
                {
                    Debug.LogError($"打开弹窗失败，未找到预制体: {popupName}");
                    onOpened?.Invoke(null);
                    return;
                }

                GameObject popupObject = Object.Instantiate(popupPrefab, _popupRoot.GetPopupCanvasTransform());
                popupObject.name = popupPrefab.name;
                popupObject.SetActive(false);

                PopupBase popup = popupObject.GetComponent<PopupBase>();
                if (popup == null)
                {
                    Debug.LogError($"打开弹窗失败，预制体 {popupName} 上未找到 PopupBase 或其子类组件");
                    onOpened?.Invoke(null);
                    return;
                }

                data.popupName = popupName;
                _popupCache[popupName] = popup;
                BringPopupToTop(popup);
                popup.Open(data);
                onOpened?.Invoke(popup);
            });
        }

        public bool TryGetOpenedPopup<TPopup>(string popupName, out TPopup popup) where TPopup : PopupBase
        {
            if (_popupCache.TryGetValue(popupName, out PopupBase cachedPopup))
            {
                if (cachedPopup != null)
                {
                    popup = cachedPopup as TPopup;
                    return popup != null;
                }

                _popupCache.Remove(popupName);
            }

            popup = null;
            return false;
        }

        public bool CloseTopPopup()
        {
            if (_popupRoot == null)
            {
                return false;
            }

            Transform popupRootTransform = _popupRoot.GetPopupCanvasTransform();
            if (popupRootTransform == null || popupRootTransform.childCount == 0)
            {
                return false;
            }

            Transform topTransform = popupRootTransform.GetChild(popupRootTransform.childCount - 1);
            PopupBase topPopup = topTransform.GetComponent<PopupBase>();
            if (topPopup == null)
            {
                return false;
            }

            string targetKey = null;
            List<string> invalidKeys = null;
            foreach (KeyValuePair<string, PopupBase> kv in _popupCache)
            {
                if (kv.Value == null)
                {
                    invalidKeys ??= new List<string>();
                    invalidKeys.Add(kv.Key);
                    continue;
                }

                if (kv.Value == topPopup)
                {
                    targetKey = kv.Key;
                }
            }

            if (invalidKeys != null)
            {
                foreach (string key in invalidKeys)
                {
                    _popupCache.Remove(key);
                }
            }

            if (!string.IsNullOrEmpty(targetKey))
            {
                _popupCache.Remove(targetKey);
            }

            topPopup.Close();
            return true;
        }

        public int CloseAllPopups()
        {
            if (_popupRoot == null)
            {
                _popupCache.Clear();
                return 0;
            }

            Transform popupRootTransform = _popupRoot.GetPopupCanvasTransform();
            if (popupRootTransform == null || popupRootTransform.childCount == 0)
            {
                _popupCache.Clear();
                return 0;
            }

            List<PopupBase> popupsToClose = new List<PopupBase>();
            for (int i = popupRootTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = popupRootTransform.GetChild(i);
                PopupBase popup = child.GetComponent<PopupBase>();
                if (popup != null)
                {
                    popupsToClose.Add(popup);
                }
            }

            _popupCache.Clear();

            int closeCount = 0;
            foreach (PopupBase popup in popupsToClose)
            {
                if (popup == null)
                {
                    continue;
                }

                popup.Close();
                closeCount++;
            }

            return closeCount;
        }
    }
}
