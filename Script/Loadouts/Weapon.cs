using HoverTanks.Networking;
using System;
using UnityEngine;

using Random = UnityEngine.Random;

namespace HoverTanks.Loadouts
{
    public interface IWeaponInfo : IEquipmentInfo
    {
        WeaponTypes WeaponType { get; }
        float ReloadTimer { get; }
        float TimeToReload { get; }
        float CurrentShotSpread { get; }
        Action ReloadStarted { get; set; }
        Action ReloadFinished { get; set; }
        Weapon.Configuration Configure();
    }

    public abstract partial class Weapon : Equipment, IWeaponInfo
    {
        public WeaponTypes WeaponType => _weaponType;
        public float ReloadTimer { get; private set; }
        public float TimeToReload => _timeToReload;
        public float CurrentShotSpread { get; protected set; }
        public Action ReloadStarted { get; set; }
        public Action ReloadFinished { get; set; }

        [Header("Base Info")]
        [SerializeField] WeaponTypes _weaponType;

        [Header("Timing")]
        [SerializeField] float _timeToReload;

        [Header("Effects")]
        [SerializeField] protected float _blowback;
        [SerializeField] protected GameObject _muzzleFlashPrefab;
        [SerializeField] protected EffectAudioSettings _shotAudio;

        [Header("Reload Effects")]
        [SerializeField] GameObject _magEjectPrefab;
		[SerializeField] GameObject _magEjectParticlePrefab;
        [SerializeField] EffectAudioSettings _reloadAudio;
		[SerializeField] EffectAudioSettings _magEjectAudio;

        [Header("Self Heat")]
        [SerializeField] protected float _selfHeatDamage;
        [SerializeField] protected float _heatTimeBeforeCool;

        protected Transform _ejectPoint;
        protected EffectsSource _reloadSource;
        protected FactoredFloat _reloadSpeed;

        private Transform[] _shootPoints;
        private int _nextShootPointIndex;
        private bool _isReloadScheduled;

        public virtual ProjectileBasicInfo GetProjectileBasicInfo() => default;

        public Configuration Configure()
        {
            return new Configuration(this);
        }

        public void Init(IEquipmentOwner owner, Transform[] shootPoints, Transform ejectPoint)
        {
            Init(owner, EquipmentType.Weapon);

            _shootPoints = shootPoints;
            _ejectPoint = ejectPoint;
            _reloadSource = AudioManager.CreateEffectsSource(ejectPoint);

            Charges = MaxCharges;
            CurrentShootPoint = GetNextShootPoint(true);

            OnInit();
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();

            if (_reloadSource != null)
            {
                Destroy(_reloadSource.gameObject);
            }
        }

        protected override void OnRechargeRequestStart()
        {
            if (_isReloadScheduled)
            {
                return;
            }

            AcknowledgeReloadRequest();
        }

        protected override void OnUpdateLocal()
        {
            HandleScheduledReload();
            HandleReload();
        }

        protected void ConsumeCharge()
        {
            if (!HasCharges)
            {
                return;
            }

            Charges -= 1;

            if (!HasCharges)
            {
                AcknowledgeReloadRequest();
            }
        }

        protected Transform GetNextShootPoint(bool peek = false)
        {
            if (_shootPoints == null || _shootPoints.Length == 0)
            {
                return null;
            }

            var shootPoint = _shootPoints[_nextShootPointIndex];

            if (!peek)
            {
                ++_nextShootPointIndex;

                if (_nextShootPointIndex >= _shootPoints.Length)
                {
                    _nextShootPointIndex = 0;
                }
            }

            CurrentShootPoint = shootPoint;

            return shootPoint;
        }

        protected void CommonWeaponEffects(Transform shootPoint)
        {
            if (_screenShake > 0)
            {
                float duration = Mathf.Max(0.15f, _screenShake);
                CameraShake.Shake(_screenShake, duration);
            }

            // shot audio
            Source.PlayAtPoint(_shotAudio, shootPoint.position);

            // blowback
            Owner.AddImpulse(-shootPoint.forward * _blowback);

            // muzzle flash
            if (_muzzleFlashPrefab != null)
            {
                var muzzleFlash = Instantiate(_muzzleFlashPrefab, shootPoint.position, Quaternion.LookRotation(shootPoint.forward, Vector3.up));
                Destroy(muzzleFlash, 3);
            }

            Triggered?.Invoke();
        }

        protected override void OnStateMsg(PawnEquipmentActionMsg.Actions state)
        {
            switch (state)
            {
                case PawnEquipmentActionMsg.Actions.ReloadStart: OnReloadStartEffect(); break;
                case PawnEquipmentActionMsg.Actions.ReloadFinish: OnReloadFinishEffect(); break;
            }
        }

        protected virtual void OnReloadStartEffect()
        {
            IsRecharging = true;

            Charges = 0;
            ReloadTimer = _timeToReload;

            ReloadStarted?.Invoke();

            // physical mag eject
            Vector3 ejectRight = _ejectPoint.right;
            Quaternion ejectRot = Quaternion.LookRotation((ejectRight + Vector3.up * 0.4f).normalized, Vector3.up);
            var magEject = Instantiate(_magEjectPrefab, _ejectPoint.position, ejectRot);
            var magEjectRb = magEject.GetComponent<Rigidbody>();
            magEjectRb.AddForce(magEject.transform.forward * Random.Range(5, 6), ForceMode.Impulse);
            magEjectRb.AddTorque(new Vector3(Random.Range(0, 1), Random.Range(0, 1), Random.Range(0, 1)), ForceMode.Impulse);
            DebrisManager.Register(magEject);

            // eject particle
            Instantiate(_magEjectParticlePrefab, _ejectPoint.position, ejectRot);

            _reloadSource.Play(_magEjectAudio);
        }

        protected virtual void OnReloadFinishEffect()
        {
            IsRecharging = false;

            Charges = MaxCharges;
            ReloadTimer = 0;

            ReloadFinished?.Invoke();

            _reloadSource.Play(_reloadAudio);
        }

        private void AcknowledgeReloadRequest()
        {
            if (_isReloadScheduled)
            {
                return;
            }

            _isReloadScheduled = true;
        }

        private void HandleScheduledReload()
        {
            if (!_isReloadScheduled)
            {
                return;
            }

            // prevent instant reload after shooting
            if (_nextProcTime - Time.time > _delayBetweenProcs * 0.66f)
            {
                return;
            }

            _isReloadScheduled = false;

            SendActionMessage(PawnEquipmentActionMsg.Actions.ReloadStart);
            OnReloadStartEffect();
        }

        private void HandleReload()
        {
            if (!IsRecharging)
            {
                return;
            }

            _reloadSpeed.Base = Time.deltaTime;
            ReloadTimer -= _reloadSpeed.Value;

            // still reloading
            if (ReloadTimer > 0)
            {
                return;
            }

            SendActionMessage(PawnEquipmentActionMsg.Actions.ReloadFinish);
            OnReloadFinishEffect();
        }
    }
}
