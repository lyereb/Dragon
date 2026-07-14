using System;
using DG.Tweening;
using Dragon.Audio;
using UnityEngine;

namespace Dragon.GameUI
{
    public enum UIDisplayEffectType
    {
        None,
        /// <summary>从 0 缩放到原始尺寸。</summary>
        ScaleIn,
        /// <summary>从透明渐变到原始透明度。</summary>
        FadeIn,
    }

    public enum UIDisplayExitEffectType
    {
        None,
        /// <summary>从当前尺寸缩放到 0。</summary>
        ScaleOut,
        /// <summary>从当前透明度渐变到透明。</summary>
        FadeOut,
    }

    /// <summary>
    /// 挂载在 UI 节点上，在 Enable 时播放可选的显示动效。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UIDisplayEffect : MonoBehaviour
    {
        private const string DefaultEnableSfxAddress = "sfx_ui_window_open";
        private const string DefaultDisableSfxAddress = "";

        #region Animation

        [SerializeField] private UIDisplayEffectType _effectType = UIDisplayEffectType.ScaleIn;
        [SerializeField] private float _duration = 0.25f;
        [SerializeField] private bool _playOnEnable = true;
        [SerializeField] private UIDisplayExitEffectType _exitEffectType = UIDisplayExitEffectType.None;
        [SerializeField] private float _exitDuration = 0.1f;
        [SerializeField] private bool _useUnscaledTime = true;

        #endregion

        #region Sfx

        [SerializeField] private bool _useDefaultSfx;
        [SerializeField] private string _enableSfxAddress;
        [SerializeField] private string _disableSfxAddress;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 1f;

        #endregion

        private RectTransform _rectTransform;
        private Vector3 _defaultScale;
        private CanvasGroup _canvasGroup;
        private float _defaultAlpha = 1f;
        private Tween _playTween;

        public bool HasExitEffect => _exitEffectType != UIDisplayExitEffectType.None;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _defaultScale = _rectTransform.localScale;

            if (_effectType == UIDisplayEffectType.FadeIn
                || _exitEffectType == UIDisplayExitEffectType.FadeOut)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                _defaultAlpha = _canvasGroup.alpha;
            }
        }

        private void OnEnable()
        {
            PlayEnableSfx();

            if (_playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            PlayDisableSfx();
            Stop();
        }

        public void Play()
        {
            KillTweenOnly();

            if (_effectType == UIDisplayEffectType.None)
            {
                ResetVisualState();
                return;
            }

            switch (_effectType)
            {
                case UIDisplayEffectType.ScaleIn:
                    PlayScaleIn();
                    break;

                case UIDisplayEffectType.FadeIn:
                    PlayFadeIn();
                    break;
            }
        }

        public void PlayExit(Action onCompleted)
        {
            KillTweenOnly();

            switch (_exitEffectType)
            {
                case UIDisplayExitEffectType.ScaleOut:
                    PlayScaleOut(onCompleted);
                    break;

                case UIDisplayExitEffectType.FadeOut:
                    PlayFadeOut(onCompleted);
                    break;

                default:
                    onCompleted?.Invoke();
                    break;
            }
        }

        public void Stop()
        {
            KillTweenOnly();
            ResetVisualState();
        }

        private void ResetVisualState()
        {
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _defaultScale;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _defaultAlpha;
            }
        }

        private void KillTweenOnly()
        {
            if (_playTween == null || !_playTween.IsActive())
            {
                _playTween = null;
                return;
            }

            _playTween.Kill();
            _playTween = null;
        }

        private void PlayScaleIn()
        {
            _rectTransform.localScale = Vector3.zero;

            float duration = Mathf.Max(_duration, 0f);
            if (duration <= 0f)
            {
                _rectTransform.localScale = _defaultScale;
                return;
            }

            _playTween = _rectTransform
                .DOScale(_defaultScale, duration)
                .SetEase(Ease.OutBack, 1.2f)
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject);
        }

        private void PlayFadeIn()
        {
            EnsureCanvasGroup();
            _canvasGroup.alpha = 0f;

            float duration = Mathf.Max(_duration, 0f);
            if (duration <= 0f)
            {
                _canvasGroup.alpha = _defaultAlpha;
                return;
            }

            _playTween = _canvasGroup
                .DOFade(_defaultAlpha, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject);
        }

        private void PlayScaleOut(Action onCompleted)
        {
            float duration = Mathf.Max(_exitDuration, 0f);
            if (duration <= 0f)
            {
                _rectTransform.localScale = Vector3.zero;
                onCompleted?.Invoke();
                return;
            }

            _playTween = _rectTransform
                .DOScale(Vector3.zero, duration)
                .SetEase(Ease.InQuad)
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _playTween = null;
                    onCompleted?.Invoke();
                });
        }

        private void PlayFadeOut(Action onCompleted)
        {
            EnsureCanvasGroup();

            float duration = Mathf.Max(_exitDuration, 0f);
            if (duration <= 0f)
            {
                _canvasGroup.alpha = 0f;
                onCompleted?.Invoke();
                return;
            }

            _playTween = _canvasGroup
                .DOFade(0f, duration)
                .SetEase(Ease.InQuad)
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _playTween = null;
                    onCompleted?.Invoke();
                });
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup != null)
            {
                return;
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _defaultAlpha = _canvasGroup.alpha;
        }

        private void PlayEnableSfx()
        {
            string address = _useDefaultSfx ? DefaultEnableSfxAddress : _enableSfxAddress;
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            AudioManager.Instance.PlaySfx(address, _sfxVolume);
        }

        private void PlayDisableSfx()
        {
            string address = _useDefaultSfx ? DefaultDisableSfxAddress : _disableSfxAddress;
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            AudioManager.Instance.PlaySfx(address, _sfxVolume);
        }
    }
}
