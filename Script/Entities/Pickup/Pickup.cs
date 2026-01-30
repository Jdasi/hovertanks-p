using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Entities
{
	public abstract class Pickup : NetworkEntity
	{
		public PickupClass PickupClass => _pickupClass;

		[SerializeField] PickupClass _pickupClass;
		[SerializeField] protected Rigidbody _rb;

		public void AddImpulse(Vector3 impulse)
		{
			_rb.AddForce(impulse, ForceMode.Impulse);
		}
    }
}
