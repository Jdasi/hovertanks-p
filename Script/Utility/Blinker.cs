using UnityEngine;

public class Blinker : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] Material _matOff;
    [SerializeField] Material _matOn;
    [SerializeField] float _blinkRate;

    private MeshRenderer _meshRenderer;
    private float _blinkTimer;
    private bool _isBlinkerOn;
    private bool _forceOn;

    public void ForceOn(bool on)
    {
        _forceOn = on;

        if (on)
        {
            // force blinker on
            _isBlinkerOn = false;
            SwapMaterial();
        }
    }

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (_forceOn)
        {
            return;
        }

        _blinkTimer -= Time.deltaTime;

        if (_blinkTimer <= 0)
        {
            _blinkTimer = _blinkRate;
            SwapMaterial();
        }
    }

    private void SwapMaterial()
    {
        _meshRenderer.material = _isBlinkerOn ? _matOff : _matOn;
        _isBlinkerOn = !_isBlinkerOn;
    }
}
