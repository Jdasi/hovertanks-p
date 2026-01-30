using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Entities
{
	public abstract class Projectile : NetworkEntity
	{
		public ProjectileClass ProjectileClass => _projectileClass;

		[SerializeField] ProjectileClass _projectileClass;

		public IdentityInfo Owner { get; private set; }

		public void Init(IdentityInfo owner)
		{
			Owner = owner;

			OnInit();
		}

		public virtual ProjectileBasicInfo GetProjectileBasicInfo() => default;

		protected virtual void OnInit() { }
	}
}
