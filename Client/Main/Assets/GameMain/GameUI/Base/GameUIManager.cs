using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Dragon.Resource;

namespace Dragon.GameUI
{
    public class GameUIManager
    {
        /// <summary>在 <see cref="CloseUI"/> 成功隐藏并回收界面后触发，供 UI 顺序队列等系统监听。</summary>
        public event System.Action<UIBase> AfterUIBaseClosed;

        #region //静态变量

        //静态变量
        protected static GameUIManager _instance = null;
        private static UIRoot _uiRoot = null;

        //静态属性
        public static GameUIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameUIManager();
                    _uiRoot = UIRoot.Create();
                    _uiRoot.Init();
                }
                return _instance;
            }
        }
        #endregion

        private readonly Dictionary<string, UIBase> _mainUICache = new Dictionary<string, UIBase>();
        private readonly Dictionary<string, UIBase> _singleUICache = new Dictionary<string, UIBase>();
        private string _currentMainUIName;

        public async UniTask OpenMainUI<T, TData>(string uiName, TData data)
            where T : UIBase<TData>
            where TData : UIBaseData
        {
            if (string.IsNullOrEmpty(uiName))
            {
                Debug.LogError("OpenMainUI failed: uiName is null or empty.");
                return;
            }

            if (_mainUICache.TryGetValue(uiName, out var targetUI) && targetUI != null)
            {
                if (!string.IsNullOrEmpty(_currentMainUIName) &&
                    _currentMainUIName != uiName &&
                    _mainUICache.TryGetValue(_currentMainUIName, out var oldCachedUI) &&
                    oldCachedUI != null)
                {
                    oldCachedUI.Hide();
                    oldCachedUI.gameObject.SetActive(false);
                }

                targetUI.gameObject.SetActive(true);
                ((T)targetUI).ShowByManager(data);
                _currentMainUIName = uiName;
                return;
            }

            GameObject prefab;
            try
            {
                prefab = await ResourceManager.Instance.LoadAssetAsync<GameObject>(uiName);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"OpenMainUI failed: can not load prefab at path {uiName}. {ex.Message}");
                return;
            }

            if (prefab == null)
            {
                Debug.LogError($"OpenMainUI failed: can not load prefab at path {uiName}.");
                return;
            }

            var uiRootTransform = GetMainUIRootTransform();
            var uiGameObject = Object.Instantiate(prefab, uiRootTransform, false);
            var uiBase = uiGameObject.GetComponent<UIBase>();
            if (uiBase == null)
            {
                Debug.LogError($"OpenMainUI failed: prefab {uiName} has no UIBase on root.");
                Object.Destroy(uiGameObject);
                return;
            }

            if (!(uiBase is T))
            {
                Debug.LogError($"OpenMainUI failed: prefab {uiName} UIBase type mismatch, expected {typeof(T).Name}.");
                Object.Destroy(uiGameObject);
                return;
            }

            if (!string.IsNullOrEmpty(_currentMainUIName) &&
                _currentMainUIName != uiName &&
                _mainUICache.TryGetValue(_currentMainUIName, out var oldUI) &&
                oldUI != null)
            {
                oldUI.Hide();
                oldUI.gameObject.SetActive(false);
            }

            ((T)uiBase).InitByManager(data);
            ((T)uiBase).ShowByManager(data);
            _mainUICache[uiName] = uiBase;
            _currentMainUIName = uiName;
        }


        public async UniTask OpenSingleUI<T, TData>(string uiName, int sortingOrder, TData data)
            where T : UIBase<TData>
            where TData : UIBaseData
        {
            if (string.IsNullOrEmpty(uiName))
            {
                Debug.LogError("OpenUI failed: uiName is null or empty.");
                return;
            }

            if (_singleUICache.TryGetValue(uiName, out var cachedUI) && cachedUI != null)
            {
                var cachedCanvas = EnsureCanvasAndRaycaster(cachedUI.gameObject);
                cachedCanvas.overrideSorting = true;
                cachedCanvas.sortingOrder = sortingOrder;

                cachedUI.gameObject.SetActive(true);
                ((T)cachedUI).ShowByManager(data);
                return;
            }

            GameObject prefab;
            try
            {
                prefab = await ResourceManager.Instance.LoadAssetAsync<GameObject>(uiName);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"OpenUI failed: can not load prefab at path {uiName}. {ex.Message}");
                return;
            }

            if (prefab == null)
            {
                Debug.LogError($"OpenUI failed: can not load prefab at path {uiName}.");
                return;
            }

            var uiRootTransform = GetSingleUIRootTransform();
            var uiGameObject = Object.Instantiate(prefab, uiRootTransform, false);
            var uiBase = uiGameObject.GetComponent<UIBase>();
            if (uiBase == null)
            {
                Debug.LogError($"OpenUI failed: prefab {uiName} has no UIBase on root.");
                Object.Destroy(uiGameObject);
                return;
            }

            if (!(uiBase is T))
            {
                Debug.LogError($"OpenUI failed: prefab {uiName} UIBase type mismatch, expected {typeof(T).Name}.");
                Object.Destroy(uiGameObject);
                return;
            }

            var canvas = EnsureCanvasAndRaycaster(uiGameObject);
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            ((T)uiBase).InitByManager(data);
            ((T)uiBase).ShowByManager(data);
            _singleUICache[uiName] = uiBase;
        }

        public void CloseUI(UIBase uiBase)
        {
            if (uiBase == null)
            {
                Debug.LogError("CloseUI failed: uiBase is null.");
                return;
            }

            var singleUIName = FindUIName(_singleUICache, uiBase);
            if (!string.IsNullOrEmpty(singleUIName))
            {
                uiBase.Hide();
                uiBase.gameObject.SetActive(false);
                AfterUIBaseClosed?.Invoke(uiBase);
                return;
            }

            var mainUIName = FindUIName(_mainUICache, uiBase);
            if (!string.IsNullOrEmpty(mainUIName))
            {
                uiBase.Hide();
                uiBase.gameObject.SetActive(false);

                if (_currentMainUIName == mainUIName)
                {
                    _currentMainUIName = null;
                }

                AfterUIBaseClosed?.Invoke(uiBase);
                return;
            }

            Debug.LogWarning($"CloseUI ignored: {uiBase.name} is not managed by GameUIManager cache.");
        }

        public bool TryGetCachedSingleUI(string uiName, out UIBase ui)
        {
            ui = null;
            return !string.IsNullOrEmpty(uiName) &&
                   _singleUICache.TryGetValue(uiName, out ui) &&
                   ui != null;
        }

        public bool TryGetCachedMainUI(string uiName, out UIBase ui)
        {
            ui = null;
            return !string.IsNullOrEmpty(uiName) &&
                   _mainUICache.TryGetValue(uiName, out ui) &&
                   ui != null;
        }

        #region helper
        public Transform GetUIRootTransform()
        {
            return _uiRoot.GetUIRootTransform();
        }

        public Transform GetMainUIRootTransform()
        {
            return _uiRoot.GetMainUIRootTransform();
        }

        public Transform GetSingleUIRootTransform()
        {
            return _uiRoot.GetSingleUIRootTransform();
        }

        private Canvas EnsureCanvasAndRaycaster(GameObject target)
        {
            var canvas = target.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = target.AddComponent<Canvas>();
            }

            if (target.GetComponent<GraphicRaycaster>() == null)
            {
                target.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        private string FindUIName(Dictionary<string, UIBase> cache, UIBase uiBase)
        {
            foreach (var pair in cache)
            {
                if (pair.Value == uiBase)
                {
                    return pair.Key;
                }
            }

            return null;
        }
    }
    #endregion
}