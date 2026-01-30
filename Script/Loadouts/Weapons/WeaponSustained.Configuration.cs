
namespace HoverTanks.Loadouts
{
    public partial class WeaponSustained
    {
        public new class Configuration : Weapon.Configuration
        {
            private WeaponSustained _weaponSustained;

            internal Configuration(WeaponSustained weaponSustained)
                : base(weaponSustained)
            {
                _weaponSustained = weaponSustained;
            }
        }
    }
}
