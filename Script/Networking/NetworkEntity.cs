using HoverTanks.Events;
using UnityEngine;

namespace HoverTanks.Networking
{
	public abstract class NetworkEntity : MonoBehaviour
	{
		public NetworkIdentity identity { get; } = new NetworkIdentity();
		public EntityType EntityType => _entityType;

		[SerializeField] EntityType _entityType;

		public void SetIdentity(ClientId clientId, PlayerId playerId, EntityId entityId, TeamId teamId)
		{
			identity.Set(clientId, playerId, entityId, teamId);
		}

		protected virtual void Awake() { }

		protected virtual void Start() { }

		protected virtual void OnDestroy()
		{
			LocalEvents.Invoke(new EntityGarbageCollectedData()
			{
				EntityType = _entityType,
				Identity = identity,
			});
		}
	}
}
