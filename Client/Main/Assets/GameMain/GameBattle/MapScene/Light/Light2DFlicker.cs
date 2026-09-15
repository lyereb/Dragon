using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dragon.Logic
{
    /// <summary>
    /// Point Light2D 闪烁效果。挂到 Light2D 所在物体上，仅在灯光 enabled 时运行。
    /// 与 Lighting2DManager 昼夜开关兼容：白天灯光关闭后自动恢复基准值。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light2D))]
    public class Light2DFlicker : MonoBehaviour
    {
        [Header("Intensity")]
        [SerializeField] private float _intensityMin = 0.7f;
        [SerializeField] private float _intensityMax = 1f;

        [Header("Outer Radius")]
        [SerializeField] private bool _affectOuterRadius;
        [SerializeField] private float _outerRadiusMin = 3.6f;
        [SerializeField] private float _outerRadiusMax = 4f;

        [Header("Flicker")]
        [SerializeField] private float _speed = 2f;
        [SerializeField] private float _noiseScale = 1f;
        [Tooltip("为 0 时在 Awake 随机生成，避免多盏灯同步闪烁")]
        [SerializeField] private float _seed;
        [SerializeField] private float _updateInterval = 0.2f;

        private Light2D _light;
        private float _restIntensity;
        private float _restOuterRadius;
        private float _noiseSeed;
        private float _updateTimer;
        private bool _wasLightEnabled;

        private void Reset()
        {
            _light = GetComponent<Light2D>();
            if (_light == null)
            {
                return;
            }

            _restIntensity = _light.intensity;
            _restOuterRadius = _light.pointLightOuterRadius;
            _intensityMax = _restIntensity;
            _intensityMin = _restIntensity * 0.7f;
            _outerRadiusMax = _restOuterRadius;
            _outerRadiusMin = _restOuterRadius * 0.9f;
        }

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _restIntensity = _light.intensity;
            _restOuterRadius = _light.pointLightOuterRadius;
            _noiseSeed = _seed != 0f ? _seed : Random.Range(0f, 100f);
        }

        private void OnDisable()
        {
            RestoreRestValues();
            _wasLightEnabled = false;
        }

        private void Update()
        {
            if (_light == null)
            {
                return;
            }

            if (!_light.enabled)
            {
                if (_wasLightEnabled)
                {
                    RestoreRestValues();
                    _wasLightEnabled = false;
                }

                return;
            }

            _wasLightEnabled = true;

            _updateTimer += Time.deltaTime;
            if (_updateTimer < _updateInterval)
            {
                return;
            }

            _updateTimer = 0f;
            ApplyFlicker();
        }

        private void ApplyFlicker()
        {
            float noise = Mathf.PerlinNoise(_noiseSeed, Time.time * _speed * _noiseScale);
            float t = Mathf.Clamp01(noise);

            _light.intensity = Mathf.Lerp(_intensityMin, _intensityMax, t);

            if (_affectOuterRadius)
            {
                _light.pointLightOuterRadius = Mathf.Lerp(_outerRadiusMin, _outerRadiusMax, t);
            }
        }

        private void RestoreRestValues()
        {
            if (_light == null)
            {
                return;
            }

            _light.intensity = _restIntensity;
            _light.pointLightOuterRadius = _restOuterRadius;
        }
    }
}
