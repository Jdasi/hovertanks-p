using HoverTanks.Entities;
using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Loadouts
{
    [CreateAssetMenu(menuName = "Loadout/Weapons/Projectile")]
    public partial class WeaponProjectile : Weapon
    {
        public ProjectileClass ProjectileClass => _projectileClass;

        [Header("Projectile Info")]
        [SerializeField] ProjectileClass _projectileClass;
        [SerializeField] bool _spoolEachShot;

        [Header("Shot Spread")]
        [SerializeField] float _stationarySpread;
        [SerializeField] float _movingSpread;

        public new Configuration Configure()
        {
            return new Configuration(this);
        }

        public override ProjectileBasicInfo GetProjectileBasicInfo()
        {
            EntityManager.TryGetProjectileBasicInfo(_projectileClass, out var info);
            return info;
        }

        protected override void OnInit()
        {
            SetDisabledActionMessages(new PawnEquipmentActionMsg.Actions[]
            {
                PawnEquipmentActionMsg.Actions.OnActivate,
                PawnEquipmentActionMsg.Actions.OnDeactivate,
                PawnEquipmentActionMsg.Actions.RechargeStart,
                PawnEquipmentActionMsg.Actions.RechargeStop,
            });
        }

        protected override void OnUpdateLocal()
        {
            base.OnUpdateLocal();

            HandleShotSpread();
        }

        protected override void OnActiveUpdateLocal()
        {
            if (WeaponType == WeaponTypes.ProjectileIndirect
                && TargetPos == default)
            {
                return;
            }

            if (!TestNextProc())
            {
                return;
            }

            ConsumeCharge();
            OnProcEffect();

            if (_spoolEachShot)
            {
                _customNextSpoolUpTime = Time.time + _delayBetweenProcs;
                StopUse();
            }
        }

        protected override void OnProcEffect()
        {
            CommonWeaponEffects(GetNextShootPoint());

            // shot message
            SendActionMessage(PawnEquipmentActionMsg.Actions.Proc, WeaponType == WeaponTypes.ProjectileIndirect ? TargetPos : default);

            if (Server.IsActive)
            {
                ServerRequestProjectile(CurrentShootPoint);

                if (_selfHeatDamage > 0)
                {
                    Owner.Life?.HeatDamage(new HeatDamageInfo(Owner.identity, _selfHeatDamage, _heatTimeBeforeCool));
                }
            }
        }

        private void ServerRequestProjectile(Transform shootPoint)
        {
            if (!Server.IsActive)
            {
                return;
            }

            Vector3 variance = CurrentShotSpread == 0 ? default : new Vector3(Random.Range(-CurrentShotSpread, CurrentShotSpread), 0, Random.Range(-CurrentShotSpread, CurrentShotSpread));

            Vector3 spawnPos;
            Quaternion spawnRot;

            if (WeaponType == WeaponTypes.ProjectileIndirect)
            {
                spawnPos = TargetPos + variance;
                spawnRot = Quaternion.identity;
            }
            else
            {
                spawnPos = shootPoint.position;
                spawnRot = variance == default ? shootPoint.rotation : Quaternion.LookRotation(shootPoint.forward + variance, Vector3.up);
            }

            float heading = JHelper.RotationToHeading(spawnRot);

            ServerSpawn.Projectile(new ServerCreateProjectileData()
            {
                Class = ProjectileClass,
                OwnerEquipmentType = EquipmentType.Weapon,
                Position = spawnPos,
                Heading = heading,
                Owner = Owner.identity,
            });
        }

        private void HandleShotSpread()
        {
            if (_stationarySpread == 0
                && _movingSpread == 0)
            {
                return;
            }

            if (Owner.StationaryTimer > 0f)
            {
                // lerp to stationary spread
                CurrentShotSpread = Mathf.Lerp(CurrentShotSpread, _stationarySpread, Time.deltaTime * 5);
            }
            else
            {
                // lerp to moving spread
                float spreadToUse = Owner.IsTurboOn ? _movingSpread * 1.5f : _movingSpread;
                CurrentShotSpread = Mathf.Lerp(CurrentShotSpread, spreadToUse, Time.deltaTime * 8);
            }
        }
    }
}
