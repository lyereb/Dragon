using System;
using Dragon.GameUI;
using UnityEngine;
using UnityEngine.UI;

public class PopupTutorialSlideData : PopupBaseData
{
}

/// <summary>
/// 教程分页弹窗：Inspector 配置 N 页，打开时只显示第一页；
/// 每页可指定镂空目标与下一步按钮（按钮自动绑定翻页/关闭）。
/// </summary>
[RequireComponent(typeof(UIHoleMaskGraphic))]
public class PopupTutorialSlide : PopupBase<PopupTutorialSlideData>
{
    [Serializable]
    public class Page
    {
        [Tooltip("该页根节点，翻页时显隐")]
        public GameObject pageRoot;

        [Tooltip("镂空代理 RectTransform；镂场景对象时需挂 UIHoleWorldTargetFollower")]
        public RectTransform holeTarget;

        [Tooltip("场景镂空对象名；非空时在显示该页时自动绑定到 holeTarget 上的 Follower")]
        public string worldHoleObjectName;

        [Tooltip("本页下一步按钮；为空则不自动绑定")]
        public Button nextButton;
    }

    [SerializeField] private Page[] _pages;
    [SerializeField] private UIHoleMaskGraphic _holeMask;

    private int _currentIndex;
    private bool _buttonsBound;

    private void Awake()
    {
        if (_holeMask == null)
        {
            _holeMask = GetComponent<UIHoleMaskGraphic>();
        }

        BindPageButtons();
    }

    protected override void OnOpen(PopupTutorialSlideData data)
    {
        _currentIndex = 0;
        RefreshPages();
    }

    protected override void OnClose()
    {
        UnbindPageButtons();
    }

    /// <summary>
    /// 显示下一页；已是最后一页则关闭弹窗。
    /// </summary>
    public void OnNextClick()
    {
        if (_pages == null || _pages.Length == 0)
        {
            Close();
            return;
        }

        if (_currentIndex >= _pages.Length - 1)
        {
            Close();
            return;
        }

        _currentIndex++;
        RefreshPages();
    }

    private void BindPageButtons()
    {
        if (_buttonsBound || _pages == null)
        {
            return;
        }

        for (int i = 0; i < _pages.Length; i++)
        {
            Page page = _pages[i];
            if (page?.nextButton == null)
            {
                continue;
            }

            page.nextButton.onClick.RemoveListener(OnNextClick);
            page.nextButton.onClick.AddListener(OnNextClick);
        }

        _buttonsBound = true;
    }

    private void UnbindPageButtons()
    {
        if (!_buttonsBound || _pages == null)
        {
            return;
        }

        for (int i = 0; i < _pages.Length; i++)
        {
            Page page = _pages[i];
            if (page?.nextButton == null)
            {
                continue;
            }

            page.nextButton.onClick.RemoveListener(OnNextClick);
        }

        _buttonsBound = false;
    }

    private void RefreshPages()
    {
        if (_pages == null)
        {
            ApplyHole(null);
            return;
        }

        RectTransform holeTarget = null;
        for (int i = 0; i < _pages.Length; i++)
        {
            Page page = _pages[i];
            if (page == null)
            {
                continue;
            }

            bool isCurrent = i == _currentIndex;
            if (page.pageRoot != null)
            {
                page.pageRoot.SetActive(isCurrent);
            }

            if (isCurrent)
            {
                holeTarget = page.holeTarget;
            }
        }

        Page currentPage = _currentIndex >= 0 && _currentIndex < _pages.Length
            ? _pages[_currentIndex]
            : null;
        ApplyWorldHole(currentPage);
        ApplyHole(holeTarget);
    }

    private void ApplyWorldHole(Page page)
    {
        if (page?.holeTarget == null)
        {
            return;
        }

        UIHoleWorldTargetFollower follower = page.holeTarget.GetComponent<UIHoleWorldTargetFollower>();
        if (follower == null)
        {
            if (!string.IsNullOrEmpty(page.worldHoleObjectName))
            {
                Debug.LogWarning(
                    $"[PopupTutorialSlide] Page world hole requires UIHoleWorldTargetFollower on {page.holeTarget.name}.");
            }

            return;
        }

        if (string.IsNullOrEmpty(page.worldHoleObjectName))
        {
            follower.ClearTarget();
            return;
        }

        Transform worldTarget = FindSceneObject(page.worldHoleObjectName);
        if (worldTarget == null)
        {
            Debug.LogWarning($"[PopupTutorialSlide] Scene object not found: {page.worldHoleObjectName}");
            follower.ClearTarget();
            return;
        }

        follower.SetTarget(worldTarget);
    }

    private static Transform FindSceneObject(string objectName)
    {
        GameObject sceneObject = GameObject.Find(objectName);
        return sceneObject != null ? sceneObject.transform : null;
    }

    private void ApplyHole(RectTransform holeTarget)
    {
        if (_holeMask == null)
        {
            return;
        }

        if (holeTarget == null)
        {
            _holeMask.ClearHole();
            return;
        }

        _holeMask.SetHoleTarget(holeTarget);
    }
}
