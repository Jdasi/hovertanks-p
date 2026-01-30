using HoverTanks.Loadouts;
using UnityEngine;

namespace HoverTanks.Entities
{
    public abstract class AIPawnController : PawnController
    {
        [Header("Base Firing Parameters")]
        [SerializeField] float _shootDelayMin;
        [SerializeField] float _shootDelayMax;
        [SerializeField] float _minBurstTime;
        [SerializeField] float _maxBurstTime;
        [SerializeField] bool _stopShootAfterProc = true;
        [SerializeField][Range(0, 1)] float _forceReloadThreshold;
        [SerializeField] protected float _maxShootDist = 40;

        protected bool _isBurstInProgress => _burstCountdown > 0;
        protected float _burstCountdown;

        private float _nextShootTime;

        protected override void OnInit()
        {
            ProcessWeapon(Pawn.WeaponInfo);
        }

        protected override void OnFixedUpdate()
        {
            if (!Pawn.WeaponInfo.IsActive
                && Pawn.WeaponInfo.Charges / (float)Pawn.WeaponInfo.MaxCharges <= _forceReloadThreshold)
            {
                Pawn.Reload();
                Pawn.StopReload();
            }
        }

        private void ProcessWeapon(IWeaponInfo weapon)
        {
            if (weapon == null)
            {
                return;
            }

            Pawn.WeaponInfo.Triggered += OnWeaponTriggered;
            Pawn.WeaponInfo.ReloadFinished += OnWeaponReloadFinished;
        }

        protected bool CanShoot()
        {
            if (Pawn.WeaponInfo == null)
            {
                return false;
            }

            if (Pawn.WeaponInfo.IsRecharging)
            {
                return false;
            }

            return Time.time >= _nextShootTime;
        }

        protected bool CanUseModule()
        {
            if (Pawn.ModuleInfo == null)
            {
                return false;
            }

            return Pawn.ModuleInfo.Charges > 0;
        }

        protected void TriggerShootCooldown(float factor = 1)
        {
            _nextShootTime = Time.time + Random.Range(_shootDelayMin, _shootDelayMax) * factor;

            RestartBurstCountdown();
        }

        protected void RestartBurstCountdown()
        {
            if (_maxBurstTime <= 0)
            {
                return;
            }

            _burstCountdown = Random.Range(_minBurstTime, _maxBurstTime);
        }

        private void OnWeaponTriggered()
        {
            if (_stopShootAfterProc)
            {
                Pawn.WeaponInfo.Triggered += Pawn.StopShoot;
            }
        }

        private void OnWeaponReloadFinished()
        {
            TriggerShootCooldown(0.5f);
        }
    }
}
