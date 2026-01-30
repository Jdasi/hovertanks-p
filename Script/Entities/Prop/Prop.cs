using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Entities
{
	public abstract class Prop : NetworkEntity
	{
		public PropClass PropClass => _propClass;

		[SerializeField] PropClass _propClass;

		public IdentityInfo Owner { get; private set; }

		public void Init(IdentityInfo owner)
		{
			Owner = owner;

			OnInit();
		}

		protected virtual void OnInit() { }
    }
}
