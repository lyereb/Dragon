using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dragon.GameUI
{
    /// <summary>
    /// TMP 描边组件，挂载在 TextMeshProUGUI / TextMeshPro 同一 GameObject 上。
    /// 编辑器中修改参数会即时预览（ExecuteAlways + OnValidate）。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("UI/TextMeshPro/UITMP Outline Effect", 200)]
    public class UITMPOutlineEffect : MonoBehaviour
    {
        #region Outline

        [SerializeField] private bool _enableOutline = true;
        [SerializeField] private Color _outlineColor = Color.black;
        [SerializeField] [Range(0f, 1f)] private float _outlineWidth = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float _outlineSoftness;

        #endregion

        #region Face

        [SerializeField] private bool _overrideFaceColor;
        [SerializeField] private Color _faceColor = Color.white;
        [SerializeField] [Range(-1f, 1f)] private float _faceDilate;

        #endregion

        #region Underlay

        [SerializeField] private bool _enableUnderlay;
        [SerializeField] private Color _underlayColor = new Color(0f, 0f, 0f, 0.5f);
        [SerializeField] [Range(-1f, 1f)] private float _underlayOffsetX;
        [SerializeField] [Range(-1f, 1f)] private float _underlayOffsetY;
        [SerializeField] [Range(-1f, 1f)] private float _underlayDilate;
        [SerializeField] [Range(0f, 1f)] private float _underlaySoftness = 0.3f;

        #endregion

        private TMP_Text _text;
        private Material _fontAssetMaterial;
        private Material _effectMaterial;

#if UNITY_EDITOR
        private bool _playModeCallbackRegistered;
        private bool _skipApplyEffect;
#endif

        public bool EnableOutline
        {
            get => _enableOutline;
            set
            {
                _enableOutline = value;
                ApplyEffect();
            }
        }

        public Color OutlineColor
        {
            get => _outlineColor;
            set
            {
                _outlineColor = value;
                ApplyEffect();
            }
        }

        public float OutlineWidth
        {
            get => _outlineWidth;
            set
            {
                _outlineWidth = Mathf.Clamp01(value);
                ApplyEffect();
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            RegisterPlayModeCallback();
#endif
            ApplyEffect();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnregisterPlayModeCallback();
#endif
        }

        private void OnDestroy()
        {
            ReleaseEffectMaterial();
        }

        private void OnValidate()
        {
            ApplyEffect();
        }

#if UNITY_EDITOR
        private void RegisterPlayModeCallback()
        {
            if (_playModeCallbackRegistered)
            {
                return;
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            _playModeCallbackRegistered = true;
        }

        private void UnregisterPlayModeCallback()
        {
            if (!_playModeCallbackRegistered)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            _playModeCallbackRegistered = false;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    _skipApplyEffect = true;
                    _effectMaterial = null;
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    _skipApplyEffect = false;
                    ApplyEffect();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    _skipApplyEffect = false;
                    _effectMaterial = null;
                    EditorApplication.delayCall -= RestoreAndApplyAfterPlayMode;
                    EditorApplication.delayCall += RestoreAndApplyAfterPlayMode;
                    break;
            }
        }

        private void RestoreAndApplyAfterPlayMode()
        {
            EditorApplication.delayCall -= RestoreAndApplyAfterPlayMode;

            if (this == null || !TryGetComponent(out _text))
            {
                return;
            }

            RestoreTextBaseMaterial();
            ApplyEffect();
        }
#endif

        /// <summary>供 Editor 或运行时手动刷新。</summary>
        public void ApplyEffect()
        {
            if (!CanApplyEffect() || !TryGetComponent(out _text))
            {
                return;
            }

            Material baseMaterial = ResolveFontAssetMaterial();
            if (baseMaterial == null)
            {
                return;
            }

            RestoreTextBaseMaterial();

            Material effectMaterial = GetOrCreateEffectMaterial(baseMaterial);
            if (effectMaterial == null)
            {
                return;
            }

            _text.fontMaterial = effectMaterial;

            ApplyOutline(effectMaterial);
            ApplyFace(effectMaterial, baseMaterial);
            ApplyUnderlay(effectMaterial);

            _text.UpdateMeshPadding();
            _text.SetVerticesDirty();
            _text.SetLayoutDirty();
            _text.SetMaterialDirty();
        }

        private bool CanApplyEffect()
        {
#if UNITY_EDITOR
            return !_skipApplyEffect;
#else
            return true;
#endif
        }

        private Material ResolveFontAssetMaterial()
        {
            if (_text == null || _text.font == null)
            {
                return null;
            }

            _fontAssetMaterial = _text.font.material;
            return _fontAssetMaterial;
        }

        private void RestoreTextBaseMaterial()
        {
            Material baseMaterial = ResolveFontAssetMaterial();
            if (baseMaterial == null)
            {
                return;
            }

            if (_text.fontSharedMaterial == null)
            {
                _text.fontSharedMaterial = baseMaterial;
            }
        }

        private Material GetOrCreateEffectMaterial(Material baseMaterial)
        {
            if (_effectMaterial == null)
            {
                _effectMaterial = new Material(baseMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            return _effectMaterial;
        }

        private void ReleaseEffectMaterial()
        {
            if (_effectMaterial == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(_effectMaterial);
            }
            else
#endif
            {
                Destroy(_effectMaterial);
            }

            _effectMaterial = null;
        }

        private void ApplyOutline(Material material)
        {
            if (_enableOutline)
            {
                material.SetColor(ShaderUtilities.ID_OutlineColor, _outlineColor);
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, _outlineWidth);
                material.SetFloat(ShaderUtilities.ID_OutlineSoftness, _outlineSoftness);
            }
            else
            {
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
            }
        }

        private void ApplyFace(Material material, Material baseMaterial)
        {
            material.SetFloat(ShaderUtilities.ID_FaceDilate, _faceDilate);

            if (_overrideFaceColor)
            {
                material.SetColor(ShaderUtilities.ID_FaceColor, _faceColor);
            }
            else if (baseMaterial != null)
            {
                material.SetColor(ShaderUtilities.ID_FaceColor,
                    baseMaterial.GetColor(ShaderUtilities.ID_FaceColor));
            }
        }

        private void ApplyUnderlay(Material material)
        {
            if (_enableUnderlay)
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                material.SetColor(ShaderUtilities.ID_UnderlayColor, _underlayColor);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, _underlayOffsetX);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, _underlayOffsetY);
                material.SetFloat(ShaderUtilities.ID_UnderlayDilate, _underlayDilate);
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, _underlaySoftness);
            }
            else
            {
                material.DisableKeyword(ShaderUtilities.Keyword_Underlay);
            }
        }
    }
}
