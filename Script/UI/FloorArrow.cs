using UnityEngine;
using UnityEngine.UI;

public class FloorArrow : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] float _wipeDuration;
    [SerializeField] float _delayBetweenWipes;
    [SerializeField] LeanTweenType _tweenType;

    [Header("References")]
    [SerializeField] Image _imgMask;
    [SerializeField] Image _imgFill;

    private float _wipeCooldown;

    private void Awake()
    {
        OnDisable();
    }

    private void OnEnable()
    {
        PrepareForWipe();
        SetWipeCooldown(0.1f);

        _imgMask.enabled = true;
        _imgFill.enabled = true;
    }

    private void OnDisable()
    {
        _imgMask.enabled = false;
        _imgFill.enabled = false;
    }

    private void Update()
    {
        _wipeCooldown -= Time.deltaTime;

        if (_wipeCooldown > 0)
        {
            return;
        }

        SetWipeCooldown();

        _imgFill.rectTransform.LeanMoveLocalY(_imgMask.rectTransform.rect.height / 2, _wipeDuration)
            .setEase(_tweenType)
            .setOnComplete(PrepareForWipe);
    }

    private void SetWipeCooldown(float factor = 1)
    {
        _wipeCooldown = (_wipeDuration + _delayBetweenWipes) * factor;
    }

    private void PrepareForWipe()
    {
        _imgFill.rectTransform.anchoredPosition = _imgMask.rectTransform.anchoredPosition - new Vector2(0, _imgFill.rectTransform.rect.height);
    }
}
