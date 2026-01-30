using HoverTanks.Entities;
using HoverTanks.Events;
using HoverTanks.Networking;
using UnityEngine;

public class VehicleDropManager : MonoBehaviour
{
	[SerializeField] HealthOrb _healthOrbPrefab;
	[SerializeField][Range(0, 100)] int _healthOrbDropChance = 25;
	[SerializeField][Range(0, 5)] int _maxHealthOrbsPerDeath = 3;

	public void Start()
	{
		LocalEvents.Subscribe<ServerPawnKilledData>(OnPawnKilled);
	}

	public void OnDestroy()
	{
		LocalEvents.Unsubscribe<ServerPawnKilledData>(OnPawnKilled);
	}

	private void OnPawnKilled(ServerPawnKilledData data)
	{
		if (!Server.IsActive)
		{
			return;
		}

		for (int i = 0; i < _maxHealthOrbsPerDeath; ++i)
		{
			if (Random.Range(0, 100) > _healthOrbDropChance)
			{
				continue;
			}

			Vector3 position = data.Victim.Position + Vector3.up * 0.5f;
			position += new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));

			Vector3 impulse = JHelper.FlatDirection(data.Victim.Position, position) * 0.3f;
			impulse += Vector3.up * 0.5f;

			ServerSpawn.Pickup(new ServerCreatePickupData()
			{
				Class = PickupClass.HealthOrb,
				Position = position,
				Impulse = impulse,
			});
		}
	}
}
