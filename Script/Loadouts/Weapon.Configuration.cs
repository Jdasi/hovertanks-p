
namespace HoverTanks.Loadouts
{
    public abstract partial class Weapon
    {
        public new class Configuration : Equipment.Configuration
        {
            private Weapon _weapon;

            internal Configuration(Weapon weapon)
                : base(weapon)
            {
                _weapon = weapon;
            }

            public Configuration AddTimeToReloadFactor(float factor)
            {
                _weapon._reloadSpeed.AddFactor(factor);
                return this;
            }

            public Configuration RemoveTimeToReloadFactor(float factor)
            {
                _weapon._reloadSpeed.RemoveFactor(factor);
                return this;
            }

            public Configuration AdjustSelfHeatDamage(float amount)
            {
                _weapon._selfHeatDamage += amount;
                return this;
            }

            public Configuration SetSelfHeatDamage(float amount)
            {
                _weapon._selfHeatDamage = amount;
                return this;
            }

            public Configuration AdjustHeatTimeBeforeCool(float amount)
            {
                _weapon._heatTimeBeforeCool += amount;
                return this;
            }

            public Configuration SetHeatTimeBeforeCool(float amount)
            {
                _weapon._heatTimeBeforeCool = amount;
                return this;
            }
        }
    }
}
