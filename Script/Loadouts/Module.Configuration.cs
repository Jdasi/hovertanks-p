
namespace HoverTanks.Loadouts
{
    public abstract partial class Module
    {
        public new class Configuration : Equipment.Configuration
        {
            private Module _module;

            internal Configuration(Module module)
                : base(module)
            {
                _module = module;
            }

            public Configuration AddTimeToRechargeFactor(float factor)
            {
                _module._rechargeSpeed.AddFactor(factor);
                return this;
            }
        }
    }
}
