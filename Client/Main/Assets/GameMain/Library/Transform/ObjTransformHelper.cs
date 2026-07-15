using UnityEngine;

/// <summary>
/// 通用本地 Transform 动效辅助。在记录的基准 localPosition / localRotation / localScale 上叠加动效。
/// 适用于场景标识箭头、提示物件等需要持续晃动、旋转或呼吸缩放的装饰物。
/// </summary>
[DisallowMultipleComponent]
public class ObjTransformHelper : MonoBehaviour
{
    [System.Flags]
    public enum DisplacementMode
    {
        None = 0,
        VerticalBob = 1 << 0,
        HorizontalBob = 1 << 1,
        RotateClockwise = 1 << 2,
        RotateCounterClockwise = 1 << 3,
        ScaleBreath = 1 << 4,
    }

    [Header("位移模式")]
    [SerializeField] private DisplacementMode _modes = DisplacementMode.None;

    [Header("上下震动")]
    [SerializeField] private float _verticalBobAmplitude = 0.15f;
    [SerializeField] private float _verticalBobFrequency = 2f;
    [SerializeField] [Range(0f, 1f)] private float _verticalBobPhaseOffset;

    [Header("左右震动")]
    [SerializeField] private float _horizontalBobAmplitude = 0.15f;
    [SerializeField] private float _horizontalBobFrequency = 2f;
    [SerializeField] [Range(0f, 1f)] private float _horizontalBobPhaseOffset;

    [Header("旋转")]
    [SerializeField] private float _rotationSpeedDegrees = 90f;
    [SerializeField] private Vector3 _rotationAxis = Vector3.forward;

    [Header("呼吸缩放")]
    [SerializeField] [Tooltip("相对基准缩放的振幅，0.1 表示在 90%~110% 之间起伏")]
    private float _scaleBreathAmplitude = 0.1f;
    [SerializeField] private float _scaleBreathFrequency = 1f;
    [SerializeField] [Range(0f, 1f)] private float _scaleBreathPhaseOffset;

    [Header("通用")]
    [SerializeField] private bool _randomizePhaseOnAwake;
    [SerializeField] private bool _useUnscaledTime;

    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private Vector3 _baseLocalScale;
    private float _elapsedTime;
    private float _rotationAngleDegrees;
    private bool _hasBaseTransform;

    public DisplacementMode Modes => _modes;

    private void Awake()
    {
        if (!_randomizePhaseOnAwake)
        {
            return;
        }

        _verticalBobPhaseOffset = Random.value;
        _horizontalBobPhaseOffset = Random.value;
        _scaleBreathPhaseOffset = Random.value;
    }

    private void OnEnable()
    {
        CaptureBaseTransform();
        _elapsedTime = 0f;
        _rotationAngleDegrees = 0f;
        ApplyTransformEffects();
    }

    private void OnDisable()
    {
        RestoreBaseTransform();
    }

    private void Update()
    {
        if (_modes == DisplacementMode.None)
        {
            return;
        }

        float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _elapsedTime += deltaTime;

        if (IsRotationEnabled())
        {
            // Unity 正角度绕轴向：沿轴正向看去为逆时针，故顺时针取负
            float direction = (_modes & DisplacementMode.RotateClockwise) != 0 ? -1f : 1f;
            _rotationAngleDegrees += _rotationSpeedDegrees * direction * deltaTime;
        }

        ApplyTransformEffects();
    }

    /// <summary>重新记录当前 localPosition / localRotation / localScale 为基准，父节点或编辑器调整后需调用。</summary>
    public void CaptureBaseTransform()
    {
        _baseLocalPosition = transform.localPosition;
        _baseLocalRotation = transform.localRotation;
        _baseLocalScale = transform.localScale;
        _hasBaseTransform = true;
    }

    /// <summary>兼容旧接口，等同 CaptureBaseTransform。</summary>
    public void CaptureBaseLocalPosition()
    {
        CaptureBaseTransform();
    }

    public void RestoreBaseTransform()
    {
        if (!_hasBaseTransform)
        {
            return;
        }

        transform.localPosition = _baseLocalPosition;
        transform.localRotation = _baseLocalRotation;
        transform.localScale = _baseLocalScale;
        _rotationAngleDegrees = 0f;
    }

    /// <summary>兼容旧接口，等同 RestoreBaseTransform。</summary>
    public void RestoreBaseLocalPosition()
    {
        RestoreBaseTransform();
    }

    public void SetModes(DisplacementMode modes)
    {
        _modes = NormalizeRotationModes(modes);
        if (_modes == DisplacementMode.None)
        {
            RestoreBaseTransform();
            return;
        }

        ApplyTransformEffects();
    }

    public void SetVerticalBobEnabled(bool enabled)
    {
        SetModeFlag(DisplacementMode.VerticalBob, enabled);
    }

    public void SetHorizontalBobEnabled(bool enabled)
    {
        SetModeFlag(DisplacementMode.HorizontalBob, enabled);
    }

    public void SetScaleBreathEnabled(bool enabled)
    {
        SetModeFlag(DisplacementMode.ScaleBreath, enabled);
    }

    public void SetRotateClockwiseEnabled(bool enabled)
    {
        if (enabled)
        {
            _modes |= DisplacementMode.RotateClockwise;
            _modes &= ~DisplacementMode.RotateCounterClockwise;
        }
        else
        {
            _modes &= ~DisplacementMode.RotateClockwise;
        }

        RefreshAfterModeChange();
    }

    public void SetRotateCounterClockwiseEnabled(bool enabled)
    {
        if (enabled)
        {
            _modes |= DisplacementMode.RotateCounterClockwise;
            _modes &= ~DisplacementMode.RotateClockwise;
        }
        else
        {
            _modes &= ~DisplacementMode.RotateCounterClockwise;
        }

        RefreshAfterModeChange();
    }

    public void ConfigureVerticalBob(float amplitude, float frequency, float phaseOffset = 0f)
    {
        _verticalBobAmplitude = amplitude;
        _verticalBobFrequency = frequency;
        _verticalBobPhaseOffset = Mathf.Clamp01(phaseOffset);
        ApplyTransformEffects();
    }

    public void ConfigureHorizontalBob(float amplitude, float frequency, float phaseOffset = 0f)
    {
        _horizontalBobAmplitude = amplitude;
        _horizontalBobFrequency = frequency;
        _horizontalBobPhaseOffset = Mathf.Clamp01(phaseOffset);
        ApplyTransformEffects();
    }

    public void ConfigureScaleBreath(float amplitude, float frequency, float phaseOffset = 0f)
    {
        _scaleBreathAmplitude = Mathf.Max(0f, amplitude);
        _scaleBreathFrequency = frequency;
        _scaleBreathPhaseOffset = Mathf.Clamp01(phaseOffset);
        ApplyTransformEffects();
    }

    public void ConfigureRotation(float speedDegrees, Vector3 axis)
    {
        _rotationSpeedDegrees = speedDegrees;
        _rotationAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward;
        ApplyTransformEffects();
    }

    private void SetModeFlag(DisplacementMode flag, bool enabled)
    {
        if (enabled)
        {
            _modes |= flag;
        }
        else
        {
            _modes &= ~flag;
        }

        RefreshAfterModeChange();
    }

    private void RefreshAfterModeChange()
    {
        if (_modes == DisplacementMode.None)
        {
            RestoreBaseTransform();
            return;
        }

        if (!IsRotationEnabled())
        {
            _rotationAngleDegrees = 0f;
            if (_hasBaseTransform)
            {
                transform.localRotation = _baseLocalRotation;
            }
        }

        if ((_modes & DisplacementMode.ScaleBreath) == 0 && _hasBaseTransform)
        {
            transform.localScale = _baseLocalScale;
        }

        ApplyTransformEffects();
    }

    private void ApplyTransformEffects()
    {
        if (!_hasBaseTransform)
        {
            return;
        }

        if (_modes == DisplacementMode.None)
        {
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = _baseLocalRotation;
            transform.localScale = _baseLocalScale;
            return;
        }

        transform.localPosition = _baseLocalPosition + CalculatePositionOffset();
        transform.localRotation = _baseLocalRotation * Quaternion.AngleAxis(_rotationAngleDegrees, GetSafeRotationAxis());
        transform.localScale = CalculateScale();
    }

    private Vector3 CalculatePositionOffset()
    {
        Vector3 offset = Vector3.zero;

        if ((_modes & DisplacementMode.VerticalBob) != 0)
        {
            float phase = (_elapsedTime * _verticalBobFrequency + _verticalBobPhaseOffset) * Mathf.PI * 2f;
            offset.y = Mathf.Sin(phase) * _verticalBobAmplitude;
        }

        if ((_modes & DisplacementMode.HorizontalBob) != 0)
        {
            float phase = (_elapsedTime * _horizontalBobFrequency + _horizontalBobPhaseOffset) * Mathf.PI * 2f;
            offset.x = Mathf.Sin(phase) * _horizontalBobAmplitude;
        }

        return offset;
    }

    private Vector3 CalculateScale()
    {
        if ((_modes & DisplacementMode.ScaleBreath) == 0)
        {
            return _baseLocalScale;
        }

        float phase = (_elapsedTime * _scaleBreathFrequency + _scaleBreathPhaseOffset) * Mathf.PI * 2f;
        float multiplier = 1f + Mathf.Sin(phase) * _scaleBreathAmplitude;
        return _baseLocalScale * Mathf.Max(0.0001f, multiplier);
    }

    private bool IsRotationEnabled()
    {
        return (_modes & (DisplacementMode.RotateClockwise | DisplacementMode.RotateCounterClockwise)) != 0;
    }

    private static DisplacementMode NormalizeRotationModes(DisplacementMode modes)
    {
        bool clockwise = (modes & DisplacementMode.RotateClockwise) != 0;
        bool counterClockwise = (modes & DisplacementMode.RotateCounterClockwise) != 0;
        if (clockwise && counterClockwise)
        {
            modes &= ~DisplacementMode.RotateCounterClockwise;
        }

        return modes;
    }

    private Vector3 GetSafeRotationAxis()
    {
        return _rotationAxis.sqrMagnitude > 0.0001f ? _rotationAxis.normalized : Vector3.forward;
    }
}
