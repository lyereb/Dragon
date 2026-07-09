using DG.Tweening;
using UnityEngine;

namespace Dragon.GameUI
{
    public enum UIDisplayEffectType
    {
        None,
        /// <summary>从 0 缩放到原始尺寸。</summary>
        ScaleIn,
    }

    /// <summary>
    /// 挂载在 UI 节点上，在 Enable 时播放可选的显示动效。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UIDisplayEffect : MonoBehaviour
    {
        [SerializeField] private UIDisplayEffectType _effectType = UIDisplayEffectType.ScaleIn;
        [SerializeField] private float _duration = 0.25f;
        [SerializeField] private bool _playOnEnable = true;
        [SerializeField] private bool _useUnscaledTime = true;

        private RectTransform _rectTransform;
        private Vector3 _defaultScale;
        private Tween _playTween;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _defaultScale = _rectTransform.localScale;
        }

        private void OnEnable()
        {
            if (_playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        public void Play()
        {
            KillTweenOnly();

            if (_effectType == UIDisplayEffectType.None)
            {
                _rectTransform.localScale = _defaultScale;
                return;
            }

            switch (_effectType)
            {
                case UIDisplayEffectType.ScaleIn:
                    PlayScaleIn();
                    break;
            }
        }

        public void Stop()
        {
            KillTweenOnly();
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _defaultScale;
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
    }
}
