
namespace HoverTanks.Entities
{
    public partial class ProjectileDirect
    {
        public class Configuration
        {
            private ProjectileDirect _projectile;

            internal Configuration(ProjectileDirect projectile)
            {
                _projectile = projectile;
            }

			public Configuration AdjustInitialForce(float amount)
			{
				_projectile._initialForce += amount;
				return this;
			}

			public Configuration AdjustMaxBounces(uint amount)
			{
				_projectile._maxBounces += amount;
				return this;
			}

			public Configuration SetMaxBounces(uint amount)
			{
				_projectile._maxBounces = amount;
				return this;
			}

			public Configuration AdjustDamage(short amount)
			{
				_projectile._damage += amount;
				return this;
			}

			public Configuration AdjustHeatDamage(float amount)
			{
				_projectile._heatDamage += amount;
				return this;
			}

			public Configuration AdjustHeatDamageTimeBeforeCool(float amount)
			{
				_projectile._heatDamageTimeBeforeCool += amount;
				return this;
			}

			public Configuration AdjustHomingStrength(float amount)
			{
				_projectile._homingStrength += amount;
				return this;
			}
        }
    }
}
