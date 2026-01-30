
public partial class ScoredEventManager
{
	private class Trackers
	{
		private readonly KillingSpreeTracker Kills;
		private readonly DamageLevelTracker DamageLevel;
		private readonly AccoladeSpreeTracker Accolades;

		public Trackers(PlayerId playerId)
		{
			Kills = new KillingSpreeTracker(playerId);
			DamageLevel = new DamageLevelTracker(playerId);
			Accolades = new AccoladeSpreeTracker(playerId);
		}

		public void Cleanup()
		{
			Kills.Cleanup();
			DamageLevel.Cleanup();
			Accolades.Cleanup();
		}
	}
}
