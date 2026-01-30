using UnityEngine;

namespace HoverTanks.Networking
{
    public static class ServerSpawn
	{
		private static EntityManager _entityManager;

		static ServerSpawn()
		{
			_entityManager = GameObject.FindFirstObjectByType<EntityManager>();
		}

		public static void Pawn(ServerCreatePawnData data)
		{
			if (!Server.IsActive)
			{
				return;
			}

			EntityId entityId = EntityManager.GetUniqueEntityId();

			if (data.PlayerId >= PlayerId.One)
			{
				Server.RegisterEntityAuthority(data.ClientId, entityId);
			}

			using (var sendMsg = new CreatePawnMsg()
			{
				Class = data.PawnClass,
				ClientId = data.ClientId,
				PlayerId = data.PlayerId,
				EntityId = entityId,
				TeamId = data.TeamId,
				Position = data.Position,
				Heading = data.Heading,
				HealthModifier = data.HealthModifier,
			})
			{
				ServerSend.ToAllExceptHost(sendMsg);
			}

			_entityManager.CreatePawn(data.PawnClass, data.ClientId, data.PlayerId, entityId, data.TeamId,
				data.Position, data.Heading, data.HealthModifier);
		}

		public static void Projectile(ServerCreateProjectileData data)
		{
			if (!Server.IsActive)
			{
				return;
			}

			EntityId entityId = EntityManager.GetUniqueEntityId();

			using (var sendMsg = new CreateProjectileMsg()
            {
                Class = data.Class,
				OwnerEquipmentType = data.OwnerEquipmentType,
                EntityId = entityId,
				OwnerEntityId = data.Owner.entityId,
                Position = data.Position,
                Heading = data.Heading,
            })
			{
				ServerSend.ToAllExceptHost(sendMsg);
			}

			_entityManager.CreateProjectile(data.Class, data.Position, data.Heading, entityId, data.OwnerEquipmentType,
				data.Owner.playerId, data.Owner.entityId, data.Owner.teamId);
		}

		public static void Pickup(ServerCreatePickupData data)
		{
			if (!Server.IsActive)
			{
				return;
			}

			EntityId entityId = EntityManager.GetUniqueEntityId();

			using (var sendMsg = new CreatePickupMsg()
            {
                Class = data.Class,
                EntityId = entityId,
                Position = data.Position,
                Impulse = data.Impulse,
            })
			{
				ServerSend.ToAllExceptHost(sendMsg);
			}

			_entityManager.CreatePickup(data.Class, data.Position, data.Impulse, entityId);
		}

		public static void Prop(ServerCreatePropData data)
		{
			if (!Server.IsActive)
			{
				return;
			}

			EntityId entityId = EntityManager.GetUniqueEntityId();

			using (var sendMsg = new CreatePropMsg()
            {
                Class = data.Class,
                EntityId = entityId,
                Position = data.Position,
				Heading = data.Heading,
            })
			{
				ServerSend.ToAllExceptHost(sendMsg);
			}

			_entityManager.CreateProp(data.Class, data.Position, data.Heading, entityId, data.Owner.playerId, data.Owner.entityId, data.Owner.teamId);
		}
	}
}
