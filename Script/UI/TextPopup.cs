using UnityEngine;
using UnityEngine.UI;

public class TextPopup : MonoBehaviour
{
    public bool HasExpired => Time.time >= _expireTime;
    public string Text => _txt.text;

    [SerializeField] Text _txt;
    [SerializeField] FadableGraphic _fade;
    [SerializeField] Animator _animator;

    private float _riseSpeed;
    private float _riseTime;
    private float _expireTime;

    public void Init(string str, float riseDuration, float hangDuration, float riseSpeed, Color color, bool startShown = true)
    {
        _txt.color = color;

        Init(str, riseDuration, hangDuration, riseSpeed, startShown);
    }

    public void Init(string str, float riseDuration, float hangDuration, float riseSpeed, bool startShown = true)
    {
        _txt.text = str;
        _riseTime = Time.time + riseDuration;
        _expireTime = _riseTime + hangDuration;
        _riseSpeed = riseSpeed;

        if (startShown)
        {
            _fade.FadeIn(0);
            _animator.Play("Popup");
        }
        else
        {
            Hide(0);
        }
    }

    public void Hide(float fadeTime = 0)
    {
        _fade.FadeOut(fadeTime);
    }

    private void Update()
    {
        if (Time.time >= _riseTime
            || _riseSpeed == 0)
        {
            return;
        }

        transform.position += transform.up * _riseSpeed * Time.deltaTime;
    }
}
