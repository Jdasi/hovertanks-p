
namespace HoverTanks.Loadouts
{
    public abstract partial class Equipment
    {
        public class Configuration
        {
            private Equipment _equipment;

            internal Configuration(Equipment equipment)
            {
                _equipment = equipment;
            }

            public Configuration AdjustMaxCharges(int amount)
            {
                _equipment._maxCharges += amount;
                return this;
            }

            public Configuration AdjustDelayBetweenProcs(float amount)
            {
                _equipment._delayBetweenProcs += amount;
                return this;
            }
        }
    }
}
