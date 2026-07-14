using DG.Tweening;
using Dragon.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dragon.GameUI
{
    public enum UIButtonEffectType
    {
        None,
        /// <summary>按下缩小，释放回弹至原始尺寸。</summary>
        ScalePressBounce,
    }

    /// <summary>
    /// 通用 UI 按钮交互反馈：按下 / 释放时的动效与音效。
    /// 需与可射线检测的 Graphic 同节点挂载（如 Image + Button）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/UIButton Effect", 200)]
    public class UIButtonEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const string DefaultPressSfxAddress = "";
        private const string DefaultReleaseSfxAddress = "sfx_ui_btn_release";

        #region Animation

        [SerializeField] private UIButtonEffectType _effectType = UIButtonEffectType.ScalePressBounce;
        [SerializeField] private float _pressDuration = 0.08f;
        [SerializeField] private float _releaseDuration = 0.2f;
        [SerializeField] [Range(0.5f, 1f)] private float _pressScale = 0.92f;
        [SerializeField] private float _releaseOvershoot = 1.2f;
        [SerializeField] private bool _useUnscaledTime = true;

        #endregion

        #region Sfx

        [SerializeField] private bool _useDefaultSfx;
        [SerializeField] private string _pressSfxAddress;
        [SerializeField] private string _releaseSfxAddress;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 1f;

        #endregion

        private RectTransform _rectTransform;
        private Button _button;
        private Vector3 _defaultScale;
        private Tween _effectTween;
        private bool _isPressed;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _button = GetComponent<Button>();
            _defaultScale = _rectTransform.localScale;
        }

        private void OnDisable()
        {
            _isPressed = false;
            StopEffectTween();
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _defaultScale;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            _isPressed = true;
            PlayPressEffect();
            PlayPressSfx();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed)
            {
                return;
            }

            _isPressed = false;

            if (!IsInteractable())
            {
                ResetScaleImmediate();
                return;
            }

            PlayReleaseEffect(playSfx: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isPressed)
            {
                return;
            }

            _isPressed = false;
            PlayReleaseEffect(playSfx: false);
        }

        private void PlayPressEffect()
        {
            if (_effectType == UIButtonEffectType.None)
            {
                return;
            }

            StopEffectTween();

            switch (_effectType)
            {
                case UIButtonEffectType.ScalePressBounce:
                    PlayScalePress();
                    break;
            }
        }

        private void PlayReleaseEffect(bool playSfx)
        {
            if (playSfx)
            {
                PlayReleaseSfx();
            }

            if (_effectType == UIButtonEffectType.None)
            {
                return;
            }

            StopEffectTween();

            switch (_effectType)
            {
                case UIButtonEffectType.ScalePressBounce:
                    PlayScaleRelease();
                    break;
            }
        }

        private void PlayScalePress()
        {
            float duration = Mathf.Max(_pressDuration, 0f);
            Vector3 targetScale = _defaultScale * _pressScale;

            if (duration <= 0f)
            {
                _rectTransform.localScale = targetScale;
                return;
            }

            _effectTween = _rectTransform
                .DOScale(targetScale, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject);
        }

        private void PlayScaleRelease()
        {
            float duration = Mathf.Max(_releaseDuration, 0f);

            if (duration <= 0f)
            {
                _rectTransform.localScale = _defaultScale;
                return;
            }

            _effectTween = _rectTransform
                .DOScale(_defaultScale, duration)
                .SetEase(Ease.OutBack, _releaseOvershoot)
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject);
        }

        private void ResetScaleImmediate()
        {
            StopEffectTween();
            _rectTransform.localScale = _defaultScale;
        }

        private void StopEffectTween()
        {
            if (_effectTween == null || !_effectTween.IsActive())
            {
                _effectTween = null;
                return;
            }

            _effectTween.Kill();
            _effectTween = null;
        }

        private void PlayPressSfx()
        {
            string address = _useDefaultSfx ? DefaultPressSfxAddress : _pressSfxAddress;
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            AudioManager.Instance.PlaySfx(address, _sfxVolume);
        }

        private void PlayReleaseSfx()
        {
            string address = _useDefaultSfx ? DefaultReleaseSfxAddress : _releaseSfxAddress;
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            AudioManager.Instance.PlaySfx(address, _sfxVolume);
        }

        private bool IsInteractable()
        {
            if (_button != null)
            {
                return _button.interactable && _button.IsActive();
            }

            return isActiveAndEnabled;
        }
    }
}
