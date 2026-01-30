using UnityEngine;

namespace HoverTanks.Entities
{
    public class HoverTankEngineSFX : MonoBehaviour
    {
        private const int LERP_SPEED = 7;

        [Header("Engine")]
        [SerializeField] float _engineVolumeLowerBound;
        [SerializeField] float _engineVolumeOutputFactor;
        [SerializeField] float _enginePitchLowerBound;
        [SerializeField] float _enginePitchOutputFactor;
        [SerializeField] EffectsSource _engineSource;

        [Header("Turbo")]
        [SerializeField] float _turboVolumeUpperBound;
        [SerializeField] float _turboNormalPitch;
        [SerializeField] float _turboOverheatingPitch;
        [SerializeField] EffectsSource _turboSource;

        [Header("Turning")]
        [SerializeField] float _turnVolumeOutputFactor;
        [SerializeField] EffectsSource _turnSource;

        private Pawn _pawn;

        public void Awake()
        {
            _pawn = GetComponentInParent<Pawn>();

            _engineSource.Play();
            _turboSource.Play();
            _turnSource.Play();
        }

        public void FixedUpdate()
        {
            // engine sound
            _engineSource.volume = Mathf.Lerp(_engineSource.volume, _engineVolumeLowerBound + _pawn.CurrentMovePower * _engineVolumeOutputFactor, LERP_SPEED * Time.fixedDeltaTime);
            _engineSource.pitch = Mathf.Lerp(_engineSource.pitch, _enginePitchLowerBound + _pawn.CurrentMovePower * _enginePitchOutputFactor, LERP_SPEED * Time.fixedDeltaTime);

            // turbo sound
            float turboVolumeTarget = _pawn.IsTurboOn ? _turboVolumeUpperBound : 0;
            _turboSource.volume = Mathf.Lerp(_turboSource.volume, turboVolumeTarget, LERP_SPEED * Time.fixedDeltaTime);

            float turboPitchTarget = _pawn.Life.IsOverheating ? _turboOverheatingPitch : _turboNormalPitch;
            _turboSource.pitch = Mathf.Lerp(_turboSource.pitch, turboPitchTarget, LERP_SPEED * Time.fixedDeltaTime);

            // turn sound
            _turnSource.volume = Mathf.Lerp(_turnSource.volume, Mathf.Abs(_pawn.CurrentTurnSpeed) * _turnVolumeOutputFactor, LERP_SPEED * Time.fixedDeltaTime);
        }
    }
}
