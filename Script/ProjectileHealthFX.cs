using HoverTanks.Entities;
using UnityEngine;

public class ProjectileHealthFX : MonoBehaviour
{
    [SerializeField] Gradient _gradient;
    [SerializeField] SpriteRenderer _spriteRenderer;
    [SerializeField] AnimationCurve _scaleCurve;
    [SerializeField] LifeForce _life;
    
    private Vector3 _startScale;

    private void Awake()
    {
        _life.OnHealthChanged += OnHealthChanged;
        _startScale = _life.transform.localScale;
    }

    private void OnHealthChanged(HealthChangedData data)
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _gradient.Evaluate(data.Percent);
        }

        float scaleEval = _scaleCurve.Evaluate(1 - data.Percent);
        _life.transform.localScale = _startScale * scaleEval;
    }
}
