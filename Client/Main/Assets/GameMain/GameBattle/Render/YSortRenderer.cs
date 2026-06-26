using UnityEngine;

/// <summary>
/// 根据世界坐标 Y 轴动态调整 Renderer.sortingOrder，不修改 Sorting Layer。
/// 挂在角色或场景物件上；渲染器在子节点时，请指定 <see cref="_sortTransform"/> 为逻辑位置节点（通常为根节点）。
/// </summary>
[DisallowMultipleComponent]
public class YSortRenderer : MonoBehaviour
{
    private const int SortingOrderMin = -32768;
    private const int SortingOrderMax = 32767;

    [SerializeField] private Renderer _renderer;
    [SerializeField] private Renderer[] _extraRenderers;
    [SerializeField] private Transform _sortTransform;
    [Tooltip("在采样 Y 上叠加的偏移，可用于对齐脚底。")]
    [SerializeField] private float _sortYOffset;
    [Tooltip("Awake 时从 Renderer 读取当前 sortingOrder 作为基准。")]
    [SerializeField] private bool _captureBaseOrderOnAwake = true;
    [SerializeField] private int _baseSortingOrder;
    [Tooltip("Y 轴换算精度，越大则同层前后差分越细。")]
    [SerializeField] private float _precision = 100f;
    [Tooltip("勾选后 Y 越大越靠前；默认 Y 越小越靠前。")]
    [SerializeField] private bool _invertY;

    private int _lastAppliedOrder = int.MinValue;
    private float _lastSortY = float.NaN;

    private void Awake()
    {
        ResolveRenderer();

        if (_captureBaseOrderOnAwake && _renderer != null)
        {
            _baseSortingOrder = _renderer.sortingOrder;
        }
    }

    private void OnEnable()
    {
        ForceRefresh();
    }

    private void LateUpdate()
    {
        TryApplySort();
    }

    /// <summary>
    /// 立即按当前位置刷新排序，例如传送后可手动调用。
    /// </summary>
    public void ForceRefresh()
    {
        _lastAppliedOrder = int.MinValue;
        _lastSortY = float.NaN;
        TryApplySort();
    }

    private void ResolveRenderer()
    {
        if (_renderer != null)
        {
            return;
        }

        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            _renderer = GetComponentInChildren<Renderer>(true);
        }
    }

    private void TryApplySort()
    {
        if (_renderer == null)
        {
            return;
        }

        float sortY = GetSortY();
        if (Mathf.Approximately(sortY, _lastSortY))
        {
            return;
        }

        int order = CalculateSortingOrder(sortY);
        if (order == _lastAppliedOrder)
        {
            _lastSortY = sortY;
            return;
        }

        ApplySortingOrder(order);
        _lastAppliedOrder = order;
        _lastSortY = sortY;
    }

    private float GetSortY()
    {
        Transform sampleTransform = _sortTransform != null ? _sortTransform : transform;
        return sampleTransform.position.y + _sortYOffset;
    }

    private int CalculateSortingOrder(float sortY)
    {
        float scaledY = sortY * Mathf.Max(_precision, 0.01f);
        int yOffset = Mathf.RoundToInt(_invertY ? scaledY : -scaledY);
        return Mathf.Clamp(_baseSortingOrder + yOffset, SortingOrderMin, SortingOrderMax);
    }

    private void ApplySortingOrder(int order)
    {
        _renderer.sortingOrder = order;

        if (_extraRenderers == null)
        {
            return;
        }

        for (int i = 0; i < _extraRenderers.Length; i++)
        {
            Renderer extraRenderer = _extraRenderers[i];
            if (extraRenderer != null)
            {
                extraRenderer.sortingOrder = order;
            }
        }
    }
}
