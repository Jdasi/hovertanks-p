using System.Diagnostics;

using Debug = UnityEngine.Debug;

public enum LogChannel
{
	Default,
	Pawn,
	Equipment,
	Weapon,
	WeaponProjectile,
	WeaponSustained,
	Module,
	LifeForce,
	GameManager,
	EntityManager,
	PlayerManager,
	Client,
	GameplayUI,
	GameClient,
	GameState,
	GameStateMainMenu,
	GameStateTestBed,
	GameStateArcade,
	ArcadeLevel,
	Server,
	Network,
	UI,
	Projectile,
	SustainedWeaponEffect,
	BeamEffect,
	StatusEffectFactory,
	StatusEffectManager,
	ScoredEventManager,
	AccoladeReadoutManager,
	UnitSelectUI,
	TextPopupManager,
	ProfileIO,
	ModShop,
	GameCamera,
	DeployableProp,
	ModdingHandle,
	PawnVisualization,
}

public static class Log
{
	[Conditional("DEBUG")]
	public static void Info(LogChannel channel, string str)
	{
		Debug.Log($"[{channel}] {str}");
	}

	[Conditional("DEBUG")]
	public static void Warning(LogChannel channel, string str)
	{
		Debug.LogWarning($"[{channel}] {str}");
	}

	[Conditional("DEBUG")]
	public static void Error(LogChannel channel, string str)
	{
		Debug.LogError($"[{channel}] {str}");
	}
}
