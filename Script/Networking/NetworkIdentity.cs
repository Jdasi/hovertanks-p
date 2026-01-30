
namespace HoverTanks.Networking
{
    public class NetworkIdentity
	{
		public ClientId clientId { get; private set; } = ClientId.Invalid;
		public PlayerId playerId { get; private set; } = PlayerId.Invalid;
		public EntityId entityId { get; private set; } = EntityId.Invalid;
		public TeamId teamId { get; private set; } = TeamId.None;

		public delegate void IdentityChanged(NetworkIdentity identity);
		public IdentityChanged OnIdentityChanged;

		public void Set(ClientId clientId, PlayerId playerId, EntityId entityId, TeamId teamId)
		{
			this.clientId = clientId;
			this.playerId = playerId;
			this.entityId = entityId;
			this.teamId = teamId;

			OnIdentityChanged?.Invoke(this);
		}

		public void ChangeAuthority(ClientId clientId, PlayerId playerId)
		{
			this.clientId = clientId;
			this.playerId = playerId;

			OnIdentityChanged?.Invoke(this);
		}
	}
}
