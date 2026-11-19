using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dragon.Logic
{
    public class Lighting2DManager
    {
        private const string GlobalLightObjectName = "GlobalLight";
        private const float DefaultTransitionDuration = 1.5f;

        private static readonly Color DayColor = Color.white;
        private static readonly Color NightColor = new Color(0.12f, 0.15f, 0.35f, 1f);

        private Light2D _globalLight;
        private readonly List<Light2D> _spotLights = new List<Light2D>();
        private readonly List<float> _spotLightRestIntensities = new List<float>();
        private TimePeriod _currentPeriod = TimePeriod.Day;
        private Tween _transitionTween;

        public TimePeriod CurrentPeriod => _currentPeriod;

        public void Initialize()
        {
            _globalLight = FindGlobalLight();
            if (_globalLight == null)
            {
                Debug.LogWarning(
                    $"Lighting2DManager.Initialize fail, can not find GameObject named '{GlobalLightObjectName}' with Light2D in scene.");
                return;
            }

            if (_globalLight.lightType != Light2D.LightType.Global)
            {
                Debug.LogWarning(
                    $"Lighting2DManager.Initialize warning, '{GlobalLightObjectName}' Light2D is not Global type.");
            }

            CollectSpotLights();
            SetPeriod(TimePeriod.Day, 0f);
        }

        /// <summary>
        /// 切换昼夜光照。duration &lt;= 0 为瞬间切换；默认约 1.5s 平滑过渡。
        /// </summary>
        public void SetPeriod(TimePeriod period, float duration = DefaultTransitionDuration)
        {
            KillTransition();
            _currentPeriod = period;

            if (duration <= 0f)
            {
                ApplyImmediate(period);
                return;
            }

            StartTransition(period, duration);
        }

        public void Clear()
        {
            KillTransition();
            _globalLight = null;
            _spotLights.Clear();
            _spotLightRestIntensities.Clear();
            _currentPeriod = TimePeriod.Day;
        }

        private static Light2D FindGlobalLight()
        {
            GameObject globalLightObject = GameObject.Find(GlobalLightObjectName);
            if (globalLightObject == null)
            {
                return null;
            }

            return globalLightObject.GetComponent<Light2D>();
        }

        private void CollectSpotLights()
        {
            _spotLights.Clear();
            _spotLightRestIntensities.Clear();

            Light2D[] allLights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include);
            for (int i = 0; i < allLights.Length; i++)
            {
                Light2D light = allLights[i];
                if (light == null || light == _globalLight)
                {
                    continue;
                }

                if (light.lightType == Light2D.LightType.Point)
                {
                    _spotLights.Add(light);
                    _spotLightRestIntensities.Add(light.intensity);
                }
            }
        }

        private void ApplyImmediate(TimePeriod period)
        {
            SetSpotFlickersEnabled(false);

            if (_globalLight != null)
            {
                _globalLight.color = period == TimePeriod.Day ? DayColor : NightColor;
            }

            bool enableSpotLights = period == TimePeriod.Night;
            for (int i = 0; i < _spotLights.Count; i++)
            {
                Light2D light = _spotLights[i];
                if (light == null)
                {
                    continue;
                }

                light.intensity = _spotLightRestIntensities[i];
                light.enabled = enableSpotLights;
            }

            SetSpotFlickersEnabled(enableSpotLights);
        }

        private void StartTransition(TimePeriod period, float duration)
        {
            Color startColor = _globalLight != null ? _globalLight.color : DayColor;
            Color targetColor = period == TimePeriod.Day ? DayColor : NightColor;
            bool toNight = period == TimePeriod.Night;

            SetSpotFlickersEnabled(false);

            float[] startIntensities = new float[_spotLights.Count];
            float[] targetIntensities = new float[_spotLights.Count];
            for (int i = 0; i < _spotLights.Count; i++)
            {
                Light2D light = _spotLights[i];
                if (light == null)
                {
                    continue;
                }

                float rest = _spotLightRestIntensities[i];
                if (toNight)
                {
                    if (!light.enabled)
                    {
                        light.intensity = 0f;
                        light.enabled = true;
                    }

                    startIntensities[i] = light.intensity;
                    targetIntensities[i] = rest;
                }
                else
                {
                    startIntensities[i] = light.enabled ? light.intensity : 0f;
                    targetIntensities[i] = 0f;
                    if (!light.enabled)
                    {
                        continue;
                    }
                }
            }

            _transitionTween = DOTween.To(
                    () => 0f,
                    t => ApplyTransition(t, startColor, targetColor, startIntensities, targetIntensities),
                    1f,
                    duration)
                .SetEase(Ease.Linear)
                .OnComplete(() => CompleteTransition(period))
                .OnKill(() => _transitionTween = null);
        }

        private void ApplyTransition(
            float t,
            Color startColor,
            Color targetColor,
            float[] startIntensities,
            float[] targetIntensities)
        {
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (_globalLight != null)
            {
                _globalLight.color = Color.Lerp(startColor, targetColor, smoothT);
            }

            for (int i = 0; i < _spotLights.Count; i++)
            {
                Light2D light = _spotLights[i];
                if (light == null || !light.enabled)
                {
                    continue;
                }

                light.intensity = Mathf.Lerp(startIntensities[i], targetIntensities[i], smoothT);
            }
        }

        private void CompleteTransition(TimePeriod period)
        {
            _transitionTween = null;
            ApplyImmediate(period);
        }

        private void KillTransition()
        {
            if (_transitionTween == null || !_transitionTween.IsActive())
            {
                _transitionTween = null;
                return;
            }

            _transitionTween.Kill();
            _transitionTween = null;
        }

        private void SetSpotFlickersEnabled(bool enabled)
        {
            for (int i = 0; i < _spotLights.Count; i++)
            {
                Light2D light = _spotLights[i];
                if (light == null)
                {
                    continue;
                }

                Light2DFlicker flicker = light.GetComponent<Light2DFlicker>();
                if (flicker != null)
                {
                    flicker.enabled = enabled;
                }
            }
        }
    }
}
