using UnityEngine;

namespace HoverTanks.Entities
{
    public class HoverTankThruster : MonoBehaviour
    {
        public ParticleSystem.MainModule Main => _particle.main;

        [SerializeField] ParticleSystem.MinMaxGradient _turboColor;
        [SerializeField] Light _light;
        [SerializeField] ParticleSystem _particle;

        private ParticleSystem.MinMaxGradient _startColor;
        private bool _isTurbo;

        private void Start()
        {
            _startColor = _particle.colorOverLifetime.color;
        }

        public void SetTurboEnabled(bool enabled)
        {
            if (_isTurbo == enabled)
            {
                return;
            }

            var module = _particle.colorOverLifetime;
            module.color = enabled ? _turboColor : _startColor;

            _isTurbo = enabled;
            _light.enabled = enabled;
            _light.color = _turboColor.color;
        }

    }
}
