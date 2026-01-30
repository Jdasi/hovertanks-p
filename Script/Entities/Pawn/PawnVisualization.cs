using HoverTanks.Loadouts;
using UnityEngine;
using UnityEngine.Events;

namespace HoverTanks.Entities
{
    public class PawnVisualization : MonoBehaviour
    {
        [SerializeField] Transform _mountPointTurret;
        [SerializeField] Color[] _tintColors;

        [Space]
        [SerializeField] ParticleSystem _healParticle;
        [SerializeField] UnityEvent _onWeaponProc;
        [SerializeField] UnityEvent _onModuleProc;

        protected Pawn _pawn;
        protected AnimData _animData;

        protected float _prevForwardDot;
        protected float _prevRightDot;

        private DamageFlasher _damageFlash;
        private IWeaponInfo _weaponInfo;
        private IModuleInfo _moduleInfo;

        public void Init(Pawn pawn, Transform rotationTransform)
        {
            if (_pawn != null)
            {
                Log.Error(LogChannel.PawnVisualization, $"Init - already setup for {_pawn} and passed {pawn}, avoid duplicate calls");
                return;
            }

            _pawn = pawn;

            // damage shake
            var shakeModule = GetComponentInChildren<ShakeModule>();
            if (shakeModule != null)
            {
                _pawn.Life.OnDamageBasic += shakeModule.ShakeMedium;
            }

            // damage flash
            _damageFlash = GetComponent<DamageFlasher>();
            if (_damageFlash != null)
            {
                _pawn.Life.OnDamageBasic += _damageFlash.Flash;
            }

            // life listeners
            _pawn.Life.OnHealBasic += OnHealBasic;
            _pawn.Life.OnDeathBasic += OnDeathBasic;

            OnInit(rotationTransform);
        }

        public void InitLoadout()
        {
            if (_pawn == null)
            {
                Log.Error(LogChannel.PawnVisualization, $"InitLoadout - not setup with pawn, call Init first");
                return;
            }

            ProcessWeapon(_pawn.WeaponInfo);
            ProcessModule(_pawn.ModuleInfo);
        }

        public bool TryGetMountPoint(MountPoint point, out Transform mount)
        {
            switch (point)
            {
                case MountPoint.Turret: mount = _mountPointTurret; break;

                default: mount = null; break;
            }

            return mount != null;
        }

        public Color[] GetTintColors()
        {
            return _tintColors;
        }

        public AnimData GetAnimData()
		{
			return _animData;
		}

        public void SetAnimData(AnimData data)
        {
            _animData = data;
        }

        private void Update()
        {
            if (_pawn == null)
            {
                return;
            }

            OnUpdate();
        }
        private void FixedUpdate()
        {
            if (_pawn == null)
            {
                return;
            }

            OnFixedUpdate();
        }

        private void OnHealBasic()
        {
            _damageFlash.Flash();
            _healParticle?.Play();
        }

        private void OnDeathBasic()
        {
            _pawn.Life.OnDamageBasic -= _damageFlash.Flash;
            _damageFlash.Cancel();
        }

        private void ProcessWeapon(IWeaponInfo weapon)
        {
            if (_weaponInfo != null)
            {
                _weaponInfo.Triggered -= OnWeaponTriggered;
            }

            _weaponInfo = weapon;

            if (weapon == null)
            {
                return;
            }

            weapon.Triggered += OnWeaponTriggered;
        }

        private void OnWeaponTriggered()
        {
            _onWeaponProc?.Invoke();
        }

        private void ProcessModule(IModuleInfo module)
        {
            if (_moduleInfo != null)
            {
                _moduleInfo.Triggered -= OnModuleTriggered;
            }

            _moduleInfo = module;

            if (module == null)
            {
                return;
            }

            module.Triggered += OnModuleTriggered;
        }

        private void OnModuleTriggered()
        {
            _onModuleProc?.Invoke();
        }

        protected virtual void OnInit(Transform rotationTransform) { }
        protected virtual void OnUpdate() { }
        protected virtual void OnFixedUpdate() { }
	}
}
