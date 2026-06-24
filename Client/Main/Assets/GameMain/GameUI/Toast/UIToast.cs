using System.Collections;
using TMPro;
using UnityEngine;

namespace Dragon.GameUI
{
    /// <summary>
    /// 单条浮动 Toast；显示一段时间后对指定对象渐隐并销毁。
    /// </summary>
    public class UIToast : MonoBehaviour
    {
        [SerializeField] private GameObject _fadeTarget;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private float _displayDuration = 2f;
        [SerializeField] private float _fadeDuration = 0.35f;

        private CanvasGroup _fadeCanvasGroup;
        private Coroutine _showRoutine;

        private void Awake()
        {
            CacheFadeCanvasGroup();
        }

        public void Show(string message, float displayDuration = -1f, float fadeDuration = -1f)
        {
            if (_messageText != null)
            {
                _messageText.text = message ?? string.Empty;
            }

            float resolvedDisplayDuration = displayDuration >= 0f ? displayDuration : _displayDuration;
            float resolvedFadeDuration = fadeDuration >= 0f ? fadeDuration : _fadeDuration;

            ResetFadeAlpha();

            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
            }

            _showRoutine = StartCoroutine(ShowRoutine(resolvedDisplayDuration, resolvedFadeDuration));
        }

        private void CacheFadeCanvasGroup()
        {
            if (_fadeTarget == null)
            {
                return;
            }

            _fadeCanvasGroup = _fadeTarget.GetComponent<CanvasGroup>();
            if (_fadeCanvasGroup == null)
            {
                _fadeCanvasGroup = _fadeTarget.AddComponent<CanvasGroup>();
            }

            _fadeCanvasGroup.blocksRaycasts = false;
            _fadeCanvasGroup.interactable = false;
        }

        private void ResetFadeAlpha()
        {
            if (_fadeCanvasGroup == null)
            {
                CacheFadeCanvasGroup();
            }

            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 1f;
            }
        }

        private IEnumerator ShowRoutine(float displayDuration, float fadeDuration)
        {
            if (displayDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(displayDuration);
            }

            if (_fadeCanvasGroup != null && fadeDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                    yield return null;
                }

                _fadeCanvasGroup.alpha = 0f;
            }

            Destroy(gameObject);
        }
    }
}
