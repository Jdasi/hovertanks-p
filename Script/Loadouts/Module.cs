using UnityEngine;

namespace HoverTanks.Loadouts
{
    public interface IModuleInfo : IEquipmentInfo
    {
        Module.Configuration Configure();
    }

    public abstract partial class Module : Equipment, IModuleInfo
    {
        public ModuleClass ModuleClass => _moduleClass;

        [Header("Class Info")]
        [SerializeField] ModuleClass _moduleClass;

        [Header("Charges")]
        [SerializeField] bool _preventRechargeInUse;

        [Header("Timing")]
        [SerializeField] float _timeToRecharge;

        [Header("Effects")]
        [SerializeField] EffectAudioSettings _procAudio;
        [SerializeField] GameObject _procEffectPrefab;
        [SerializeField] float _blowback;

        [Header("Physical")]
        [SerializeField] PhysicalModule _physicalPrefab;
        [SerializeField] MountPoint _mountPoint;

        protected Transform ProcPoint { get; private set; }

        private float _rechargeCountdown;
        private FactoredFloat _rechargeSpeed;

        public Configuration Configure()
        {
            return new Configuration(this);
        }

        public void Init(IModuleOwner owner, Transform procPoint)
        {
            Init(owner, EquipmentType.Module);

            Charges = MaxCharges;
            _rechargeCountdown = _timeToRecharge;

            // create physical representation
            if (_physicalPrefab != null
                && _mountPoint != MountPoint.None
                && owner.TryGetMountPoint(_mountPoint, out var mountPoint))
            {
                var physicalModule = Instantiate(_physicalPrefab, mountPoint);
                physicalModule.Recolor(owner.GetTintColors());
                ProcPoint = CurrentShootPoint = physicalModule.ActivatePoint;

                // ensure the mount is active
                if (!mountPoint.gameObject.activeInHierarchy)
                {
                    mountPoint.gameObject.SetActive(true);
                }
            }
            // no physical representation
            else
            {
                ProcPoint = CurrentShootPoint = procPoint;
            }

            OnInit();
        }

        protected override void OnUpdateLocal()
        {
            HandleRecharge();
        }

        protected void ConsumeCharge()
        {
            if (!HasCharges)
            {
                return;
            }

            Charges -= 1;
        }

        protected void CommonModuleEffects()
        {
            if (_screenShake > 0)
            {
                float duration = Mathf.Max(0.15f, _screenShake);
                CameraShake.Shake(_screenShake, duration);
            }

            // proc audio
            Source.PlayAtPoint(_procAudio, ProcPoint.position);

            // blowback
            Owner.AddImpulse(-ProcPoint.forward * _blowback);

            // muzzle flash
            if (_procEffectPrefab != null)
            {
                var muzzleFlash = Instantiate(_procEffectPrefab, ProcPoint.position, Quaternion.LookRotation(ProcPoint.forward, Vector3.up));
                Destroy(muzzleFlash, 3);
            }

            // enforce minimum recharge delay after trigger
            _rechargeCountdown = Mathf.Max(_rechargeCountdown, _timeToRecharge * 0.5f);

            Triggered?.Invoke();
        }

        private void HandleRecharge()
        {
            if (Charges == MaxCharges)
            {
                return;
            }

            if (IsInUse
                && _preventRechargeInUse)
            {
                return;
            }

            _rechargeSpeed.Base = Time.deltaTime;
            _rechargeCountdown -= _rechargeSpeed.Value;

            if (_rechargeCountdown > 0)
            {
                return;
            }

            _rechargeCountdown = _timeToRecharge;

            ++Charges;
        }
    }
}
