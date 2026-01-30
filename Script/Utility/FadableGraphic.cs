using UnityEngine;
using UnityEngine.UI;

// Attach to a GameObject with an Image, SpriteRenderer, or Text component.
public class FadableGraphic : MonoBehaviour
{
    public bool IsFading => _fading;

    public bool PulseMode;
    public float PulseSpeed;
    public float PulseLow;
    public float PulseHigh = 1;

    private Image _image;
    private SpriteRenderer _sprite;
    private Text _text;
    private CanvasGroup _group;

    private bool _fading;
    private float _fadeProgress;
    private float _fadeDuration;

    private Color _startColor;
    private Color _targetColor;

    public void SetBaseColor(Color color)
    {
        if (_image != null)
        {
            _image.color = color;
        }

        if (_sprite != null)
        {
            _sprite.color = color;
        }

        if (_text != null)
        {
            _text.color = color;
        }
    }

    public void FadeColor(Color from, Color to, float t)
    {
        _startColor = from;
        _targetColor = to;

        _fadeDuration = t;

        StartFade();
    }

    public void FadeColor(Color to, float t)
    {
        FadeColor(GetGraphicColor(), to, t);
    }

    public void FadeAlpha(float from, float to, float t)
    {
        Color fromColor = GetGraphicColor();
        fromColor.a = from;

        Color toColor = fromColor;
        toColor.a = to;

        FadeColor(fromColor, toColor, t);
    }

    public void FadeAlpha(float to, float t)
    {
        FadeAlpha(GetGraphicColor().a, to, t);
    }

    public void FadeIn(float t)
    {
        FadeAlpha(0, 1, t);
    }

    public void FadeOut(float t)
    {
        FadeAlpha(1, 0, t);
    }

    public void FadeFrom(Color color, float t)
    {
        Color from = color;
        Color to = GetGraphicColor();

        FadeColor(from, to, t);
    }

    public void FadeTo(Color color, float t)
    {
        Color from = GetGraphicColor();
        Color to = color;

        FadeColor(from, to, t);
    }

    // Detect what sort of graphic we are.
    public void Init()
    {
        if ((_image = GetComponent<Image>()) != null)
        {
            return;
        }

        if ((_sprite = GetComponent<SpriteRenderer>()) != null)
        {
            return;
        }

        if ((_text = GetComponent<Text>()) != null)
        {
            return;
        }

        if ((_group = GetComponent<CanvasGroup>()) != null)
        {
            return;
        }
    }

    void Awake()
    {
        Init();
    }

    void Update()
    {
        if (_fading)
        {
            HandleFade();
        }
        else if (PulseMode)
        {
            HandlePulse();
        }
    }

    void HandleFade()
    {
        float dt = Time.deltaTime;

        // avoid 0-division
        if (_fadeDuration <= 0)
        {
            _fadeProgress = dt;
        }

        _fadeProgress += dt;
        Color color = Color.Lerp(_startColor, _targetColor, _fadeProgress / _fadeDuration);

        if (_image)
        {
            _image.color = color;
        }
        else if (_sprite)
        {
            _sprite.color = color;
        }
        else if (_text)
        {
            _text.color = color;
        }
        else if (_group)
        {
            _group.alpha = color.a;
        }

        // Determine if fade is complete.
        if (color == _targetColor)
        {
            StopFade();
        }
    }

    void HandlePulse()
    {
        float alpha_pulse = (1 + PulseLow) + Mathf.Sin(Time.time * PulseSpeed);

        if (alpha_pulse > PulseHigh)
        {
            alpha_pulse = PulseHigh;
        }

        Color color = new Color();

        if (_image)
        {
            color = _image.color;
        }
        else if (_sprite)
        {
            color = _sprite.color;
        }
        else if (_text)
        {
            color = _text.color;
        }

        color.a = alpha_pulse;

        if (_image)
        {
            _image.color = color;
        }
        else if (_sprite)
        {
            _sprite.color = color;
        }
        else if (_text)
        {
            _text.color = color;
        }
        else if (_group)
        {
            _group.alpha = color.a;
        }
    }

    void StartFade()
    {
        if (_image)
        {
            _image.color = _startColor;
        }
        else if (_sprite)
        {
            _sprite.color = _startColor;
        }
        else if (_text)
        {
            _text.color = _startColor;
        }
        else if (_group)
        {
            _group.alpha = _startColor.a;
        }

        _fadeProgress = 0;
        _fading = true;
    }

    public void StopFade()
    {
        _fading = false;
        _fadeProgress = 0;
    }

    Color GetGraphicColor()
    {
        if (_image)
        {
            return _image.color;
        }
        else if (_sprite)
        {
            return _sprite.color;
        }
        else if (_text)
        {
            return _text.color;
        }
        else if (_group)
        {
            Color color = Color.white;
            color.a = _group.alpha;
            return color;
        }
        else
        {
            return default;
        }
    }

}
