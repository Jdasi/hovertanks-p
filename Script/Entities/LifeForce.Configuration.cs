using UnityEngine;

namespace HoverTanks.Entities
{
    public partial class LifeForce
    {
        public class Configuration
        {
            private readonly LifeForce _life;

            internal Configuration(LifeForce life)
            {
                _life = life;
            }

            public Configuration AdjustMaxHealth(int amount)
			{
				_life._maxHealth += amount;
				_life._health = Mathf.Max(_life._health + amount, 1);

				_life.OnHealthChanged?.Invoke(new HealthChangedData(_life.GetHealthPercent(), _life.GetDamageLevel(), false));

				return this;
			}

			public Configuration AddElementFactor(ElementType type, float directFactor, float aoeFactor)
			{
				// abort if no change
				if (directFactor == 1
					&& aoeFactor == 1)
				{
					return this;
				}

				// ensure data exists for type
				if (!_life.GetElementFactorData(type, out var data))
				{
					data = new ElementFactorData()
					{
						Type = type,
						Direct = new FactoredFloat(1),
						AOE = new FactoredFloat(1),
					};

					_life._elementFactors.Add(data);
				}

				data.Direct.AddFactor(directFactor);
				data.AOE.AddFactor(aoeFactor);

				return this;
			}

			public Configuration AddHeatDamageFactor(float amount)
			{
				_life._heatDamageFactor.AddFactor(amount);
				return this;
			}
        }
    }
}
