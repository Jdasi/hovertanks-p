using HoverTanks.Effects;
using HoverTanks.Entities;
using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Weapons/Sustained")]
    public partial class WeaponSustained : Weapon
    {
        [Header("Sustained Settings")]
        [SerializeField] float _chargeConsumeInterval;
        [SerializeField] float _continuousBlowback;
        [SerializeField] SustainedWeaponEffect _effectPrefab;

        private SustainedWeaponEffect _activeEffect;
        private float _activeTimer;

        public new Configuration Configure()
        {
            return new Configuration(this);
        }

        public override ProjectileBasicInfo GetProjectileBasicInfo()
        {
            if (_effectPrefab == null)
            {
                return default;
            }

            return _effectPrefab.GetProjectileBasicInfo();
        }

        protected override void OnInit()
        {
            SetDisabledActionMessages(new PawnEquipmentActionMsg.Actions[]
            {
                PawnEquipmentActionMsg.Actions.RechargeStart,
                PawnEquipmentActionMsg.Actions.RechargeStop,
            });
        }

        protected override void OnCleanup()
        {
            CleanupEffect();
        }

        protected override void OnActivate()
        {
            var shootPoint = GetNextShootPoint();

            CommonWeaponEffects(shootPoint);

            if (_activeEffect != null)
            {
                Log.Error(LogChannel.WeaponSustained, $"OnActivate - effect already existed");
                return;
            }

            _activeEffect = Instantiate(_effectPrefab);
            _activeEffect.Init(Owner.identity, shootPoint);
        }

        protected override void OnActiveUpdate()
        {
            _activeTimer += Time.deltaTime;

            while (_activeTimer >= _chargeConsumeInterval)
            {
                ConsumeCharge();
                _activeTimer -= _chargeConsumeInterval;
            }

            if (Server.IsActive
                && _selfHeatDamage > 0)
            {
                Owner.Life?.HeatDamage(new HeatDamageInfo(Owner.identity, _selfHeatDamage * Time.deltaTime, _heatTimeBeforeCool));
            }
        }

        protected override void OnActiveFixedUpdate()
        {
            if (_continuousBlowback > 0)
            {
                Owner.AddForce(-CurrentShootPoint.forward * _continuousBlowback);
            }
        }

        protected override void OnDeactivate()
        {
            CleanupEffect();
        }

        private void CleanupEffect()
        {
            if (_activeEffect == null)
            {
                return;
            }

            _activeEffect.End();

            Destroy(_activeEffect.gameObject);
            _activeEffect = null;
        }
    }
}
