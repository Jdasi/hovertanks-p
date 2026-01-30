using HoverTanks.Entities;
using UnityEngine;

public class TeslaAnimator : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] Gradient _chargeColor;
    [SerializeField] AnimationCurve _intensityCurve;

    [Header("References")]
    [SerializeField] Pawn _pawn;
    [SerializeField] MeshRenderer _orbMesh;
    [SerializeField] ParticleSystem _flashParticle;
    [SerializeField] Light _light;

    private Material _material;

    private void Start()
    {
        _material = _orbMesh.material;
        ResetAnimation();

        _pawn.ModuleInfo.SpoolUpStarted += ResetAnimation;
        _pawn.ModuleInfo.Deactivated += ResetAnimation;
        _pawn.ModuleInfo.Triggered += OnModuleTriggered;
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
        }
    }

    private void Update()
    {
        if (_pawn.ModuleInfo.IsSpoolingUp)
        {
            SetAnimProgress(_pawn.ModuleInfo.SpoolTimer / _pawn.ModuleInfo.SpoolUpTime);
        }
        else if (_pawn.ModuleInfo.IsSpoolingDown)
        {
            SetAnimProgress(1 - (_pawn.ModuleInfo.SpoolTimer / _pawn.ModuleInfo.SpoolDownTime));
        }
    }

    private void ResetAnimation()
    {
        SetAnimProgress(0);
    }

    private void OnModuleTriggered()
    {
        SetAnimProgress(1);
        _flashParticle.Play();
    }

    private void SetAnimProgress(float charge)
    {
        Color color = _chargeColor.Evaluate(charge) * _intensityCurve.Evaluate(charge);

        _material.color = color;
        _material.SetColor("_EmissionColor", color);
        _light.intensity = 1 + charge;
    }
}
