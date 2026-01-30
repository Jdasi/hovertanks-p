
namespace HoverTanks.Loadouts
{
    public partial class WeaponProjectile
    {
        public new class Configuration : Weapon.Configuration
        {
            private WeaponProjectile _weaponProjectile;

            internal Configuration(WeaponProjectile weaponProjectile)
                : base(weaponProjectile)
            {
                _weaponProjectile = weaponProjectile;
            }

            public Configuration SetProjectileClass(ProjectileClass @class)
            {
                _weaponProjectile._projectileClass = @class;
                return this;
            }

            public Configuration AdjustStationarySpread(float amount)
            {
                _weaponProjectile._stationarySpread += amount;
                return this;
            }

            public Configuration SetStationarySpread(float amount)
            {
                _weaponProjectile._stationarySpread = amount;
                return this;
            }

            public Configuration AdjustMovingSpread(float amount)
            {
                _weaponProjectile._movingSpread += amount;
                return this;
            }

            public Configuration SetMovingSpread(float amount)
            {
                _weaponProjectile._movingSpread = amount;
                return this;
            }
        }
    }
}
