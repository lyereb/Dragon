using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class UISpriteAnimator : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _frameRate = 10f;
    [SerializeField] private bool _loop = true;
    [SerializeField] private bool _playOnEnable = true;

    private int _frameIndex;
    private float _timer;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;
    public bool Loop
    {
        get => _loop;
        set => _loop = value;
    }

    private void Awake()
    {
        if (_image == null)
        {
            _image = GetComponent<Image>();
        }
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

    public void Play(bool restart = true)
    {
        if (_frames == null || _frames.Length == 0)
        {
            return;
        }

        if (restart)
        {
            _frameIndex = 0;
            _timer = 0f;
            ApplyFrame();
        }

        _isPlaying = true;
    }

    public void Stop()
    {
        _isPlaying = false;
    }

    public void SetPlaying(bool playing)
    {
        if (playing)
        {
            Play(false);
        }
        else
        {
            Stop();
        }
    }

    private void Update()
    {
        if (!_isPlaying || _frames == null || _frames.Length <= 1)
        {
            return;
        }

        if (!_loop && _frameIndex >= _frames.Length - 1)
        {
            _isPlaying = false;
            return;
        }

        _timer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(_frameRate, 0.01f);
        while (_timer >= frameDuration)
        {
            _timer -= frameDuration;
            AdvanceFrame();
            ApplyFrame();

            if (!_loop && _frameIndex >= _frames.Length - 1)
            {
                _isPlaying = false;
                break;
            }
        }
    }

    private void AdvanceFrame()
    {
        if (_loop)
        {
            _frameIndex = (_frameIndex + 1) % _frames.Length;
            return;
        }

        if (_frameIndex < _frames.Length - 1)
        {
            _frameIndex++;
        }
    }

    private void ApplyFrame()
    {
        if (_image == null || _frames == null || _frames.Length == 0)
        {
            return;
        }

        _image.sprite = _frames[_frameIndex];
    }
}
