using HoverTanks.Events;
using HoverTanks.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ScoredEventManager : MonoBehaviour, IScoredEventManager
{
	public static IScoredEventManager instance => _instance;

	[SerializeField] TextPopup _scorePopupPrefab;
    [SerializeField] EffectAudioSettings _enemyDefeatedAudio;

	private static ScoredEventManager _instance;
    private const float SILENCE_BUFFER = 0.05f;

	private IReadOnlyDictionary<int, AccoladeInfo> _accolades;
	private List<Trackers> _allTrackers;
	private DamageHistory _damageHistory;

    private List<TextPopup> _activePopups;
    private Stack<TextPopup> _inactivePopups;
    private float _nextSoundTime;

    public bool TryGetAccoladeInfo(AccoladeType type, out AccoladeInfo info)
    {
        return _accolades.TryGetValue((int)type, out info);
    }

    public int GetAccoladeThreshold(AccoladeType type)
    {
        if (!TryGetAccoladeInfo(type, out var info))
		{
			return int.MaxValue;
		}

		return info.Threshold;
    }

	private void Awake()
	{
		if (_instance != null)
		{
			Log.Error(LogChannel.ScoredEventManager, "Ctor - instance already valid");
			return;
		}

		_instance = this;

		_activePopups = new List<TextPopup>();
		_inactivePopups = new Stack<TextPopup>();
		_damageHistory = new DamageHistory();
		_allTrackers = new List<Trackers>()
		{
			new Trackers(PlayerId.One),
			new Trackers(PlayerId.Two),
			new Trackers(PlayerId.Three),
			new Trackers(PlayerId.Four),
		};

		InitAccolades(out _accolades);

        NetworkEvents.Subscribe<CreditsAwardedMsg>(OnCreditsAwardedMsg);

		LocalEvents.Subscribe<ServerPawnKilledData>(OnPawnKilled);
        LocalEvents.Subscribe<SceneChangeData>(OnSceneChange);
	}

	private void OnDestroy()
	{
		if (_instance != this)
		{
			return;
		}

		_instance = null;
		_damageHistory.Cleanup();

        for (int i = 0; i < _allTrackers.Count; ++i)
		{
            _allTrackers[i].Cleanup();
		}

		NetworkEvents.Unsubscribe<CreditsAwardedMsg>(OnCreditsAwardedMsg);

		LocalEvents.Unsubscribe<SceneChangeData>(OnSceneChange);
		LocalEvents.Unsubscribe<ServerPawnKilledData>(OnPawnKilled);
	}

	private void OnPawnKilled(ServerPawnKilledData data)
	{
		// ignore non-player kills
		if (data.Killer.PlayerId < PlayerId.One)
		{
			return;
		}

		// ignore killing self
		if (data.Victim.identity.playerId == data.Killer.PlayerId)
		{
			return;
		}

		// ignore team kills
		if (JHelper.SameTeam(data.Victim.identity.teamId, data.Killer.TeamId))
		{
			return;
		}

		// standard kill score
		int score = data.Victim.Life.MaxHealth;
		AwardCredits(data.Killer.PlayerId, AwardCreditsReason.EnemyDefeated, score, true, data.Victim.Position);

		// killed by void
		if (data.Element == ElementType.Void)
		{
			AwardAccolade(data.Killer.PlayerId, AccoladeType.Eviction);

			// more special kill accolades would be confusing
			return;
		}

		if (data.Victim.WeaponInfo != null)
		{
			// killed while recharging
			if (data.Victim.WeaponInfo.IsRecharging)
			{
				AwardAccolade(data.Killer.PlayerId, AccoladeType.Riposte);
			}
			// killed while spooling up weapon
			else if (data.Victim.WeaponInfo.IsSpoolingUp)
			{
				AwardAccolade(data.Killer.PlayerId, AccoladeType.Interrupt);
			}
		}

		// killed while overheating and not responsible for it
		if (data.Victim.Life.IsOverheating
			&&
			(data.Victim.Life.LastExternalHeatDamageInfo.Inflictor.PlayerId != data.Killer.PlayerId
			|| data.Victim.Life.LastExternalHeatDamageInfo.HasExpired))
		{
			AwardAccolade(data.Killer.PlayerId, AccoladeType.ChillOut);
		}

		// killed with a projectile
		if (data.ProjectileStats != null)
		{
			// killed with a bounce shot
			if (data.ProjectileStats.NumWallBounces > 0)
			{
				AwardAccolade(data.Killer.PlayerId, AccoladeType.WallBounce);
			}

			// killed with a long shot
			if (data.ProjectileStats.DistanceTravelled >= 15)
			{
				AwardAccolade(data.Killer.PlayerId, AccoladeType.LongShot);
			}
			// killed at close range
			else if (data.ProjectileStats.NumWallBounces == 0
				&& data.ProjectileStats.DistanceTravelled <= 3)
			{
				AwardAccolade(data.Killer.PlayerId, AccoladeType.DodgeThis);
			}
		}

		// killed by ramming
		if (data.Element == ElementType.Ram)
		{
			AwardAccolade(data.Killer.PlayerId, AccoladeType.Demolition);
		}

		// killed by overheat tick
		if (data.Element == ElementType.Overheat
			&& data.Victim.Life.LastExternalDamageInfo.Inflictor.PlayerId == data.Killer.PlayerId)
		{
			AwardAccolade(data.Killer.PlayerId, AccoladeType.Sabotage);
		}

		// accolades based on killer pawn
		if (EntityManager.ActivePawns.TryGetValue(data.Killer.EntityId, out var killerPawn))
		{
			// killed while self heat level critical
			if (killerPawn.Life.GetHeatLevel() >= HeatLevel.Critical)
			{
				AwardAccolade(data.Killer.PlayerId, AccoladeType.HotHead);
			}
		}

		// check if protected an ally
		if (_damageHistory.HasRecentlyDamagedAliveAttackerAlly(data.Victim.identity.entityId, data.Killer))
		{
			AwardAccolade(data.Killer.PlayerId, AccoladeType.CoveringFire);
		}

		// award all allies who assisted
		if (_damageHistory.TryGetAttackerAlliesOfVictim(data.Victim.identity.entityId, data.Killer, out var alliedPlayers))
		{
			foreach (var player in alliedPlayers)
			{
				AwardAccolade(player, AccoladeType.Assist);
			}
		}
	}

	private static void AwardCredits(PlayerId playerId, AwardCreditsReason reason, int amount, bool hasPosition, Vector3 position)
	{
		if (!Server.IsActive)
		{
			return;
		}

		using (var sendMsg = new CreditsAwardedMsg()
		{
			PlayerId = playerId,
			Reason = reason,
			Amount = amount,
			HasPosition = hasPosition,
			Position = position,
		})
        {
            ServerSend.ToAll(sendMsg);
        }
	}

	private static void AwardAccolade(PlayerId playerId, AccoladeType accolade)
	{
		if (!Server.IsActive)
		{
			return;
		}

		using (var sendMsg = new AccoladeAwardedMsg()
		{
			PlayerId = playerId,
			AccoladeType = accolade,
		})
        {
            ServerSend.ToAll(sendMsg);
        }
	}

	private void InitAccolades(out IReadOnlyDictionary<int, AccoladeInfo> accolades)
	{
		var dict = new Dictionary<int, AccoladeInfo>(Enum.GetNames(typeof(AccoladeType)).Length);

		RegisterAccolade(dict, AccoladeType.DoubleKill,			50,			2);
		RegisterAccolade(dict, AccoladeType.TripleKill,			100,		3);
		RegisterAccolade(dict, AccoladeType.MegaKill,			200,		4);
		RegisterAccolade(dict, AccoladeType.UltraKill,			300,		5);
		RegisterAccolade(dict, AccoladeType.Overkill,			400,		6);
		RegisterAccolade(dict, AccoladeType.KillCrazy,			500,		7);
		RegisterAccolade(dict, AccoladeType.KillingSpree,		50,			5);
		RegisterAccolade(dict, AccoladeType.Formidable,			100,		10);
		RegisterAccolade(dict, AccoladeType.Dominating,			200,		15);
		RegisterAccolade(dict, AccoladeType.Unstoppable,		300,		20);
		RegisterAccolade(dict, AccoladeType.Invincible,			400,		25);
		RegisterAccolade(dict, AccoladeType.Godlike,			500,		30);
		RegisterAccolade(dict, AccoladeType.Omnipotent,			600,		35);
		RegisterAccolade(dict, AccoladeType.TheOne,				700,		40);
		RegisterAccolade(dict, AccoladeType.Demolition,			50);
		RegisterAccolade(dict, AccoladeType.RoadRage,			100,		5);
		RegisterAccolade(dict, AccoladeType.WreckingBall,		250,		10);
		RegisterAccolade(dict, AccoladeType.BeTheBullet,		500,		15);
		RegisterAccolade(dict, AccoladeType.WallBounce,			20);
		RegisterAccolade(dict, AccoladeType.Planner,			50,			5);
		RegisterAccolade(dict, AccoladeType.Tactician,			150,		10);
		RegisterAccolade(dict, AccoladeType.Strategist,			300,		15);
		RegisterAccolade(dict, AccoladeType.LongShot,			20);
		RegisterAccolade(dict, AccoladeType.Sniper,				50,			5);
		RegisterAccolade(dict, AccoladeType.EagleEye,			150,		10);
		RegisterAccolade(dict, AccoladeType.WizzBang,			300,		15);
		RegisterAccolade(dict, AccoladeType.Riposte,			20);
		RegisterAccolade(dict, AccoladeType.Interrupt,			100);
		RegisterAccolade(dict, AccoladeType.Eviction,			200);
		RegisterAccolade(dict, AccoladeType.Sabotage,			50);
		RegisterAccolade(dict, AccoladeType.ChillOut,			50);
		RegisterAccolade(dict, AccoladeType.HotHead,			50);
		RegisterAccolade(dict, AccoladeType.DodgeThis,			20);
		RegisterAccolade(dict, AccoladeType.CloseQuarters,		50,			5);
		RegisterAccolade(dict, AccoladeType.SwiftJustice,		150,		10);
		RegisterAccolade(dict, AccoladeType.DownTheBarrel,		300,		15);
		RegisterAccolade(dict, AccoladeType.QuickRepair,		20,			10);
		RegisterAccolade(dict, AccoladeType.Scavenger,			100,		30);
		RegisterAccolade(dict, AccoladeType.ComebackKid,		250,		60);
		RegisterAccolade(dict, AccoladeType.FromTheBrink,		50);
		RegisterAccolade(dict, AccoladeType.NotEvenClose,		50);
		RegisterAccolade(dict, AccoladeType.Perfectionist,		100);
		RegisterAccolade(dict, AccoladeType.Assist,				20);
		RegisterAccolade(dict, AccoladeType.CoveringFire,		50);
		RegisterAccolade(dict, AccoladeType.Saviour,			100,		5);
		RegisterAccolade(dict, AccoladeType.Protector,			250,		10);
		RegisterAccolade(dict, AccoladeType.GuardianAngel,		400,		15);

		accolades = dict;
	}

	private void RegisterAccolade(Dictionary<int, AccoladeInfo> dict,
		AccoladeType type, short score, short threshold = 0)
	{
		dict.Add((int)type, new AccoladeInfo(type, score, threshold));
	}

	private void OnCreditsAwardedMsg(CreditsAwardedMsg msg)
    {
        if (!PlayerManager.GetPlayerInfo(msg.PlayerId, out var playerInfo))
        {
            return;
        }

        TextPopup popup;

        if (_inactivePopups.Count > 0)
        {
            popup = _inactivePopups.Pop();
        }
        else
        {
            popup = Instantiate(_scorePopupPrefab);
            popup.transform.SetParent(transform);
        }

        popup.transform.position = msg.Position + (Vector3.forward * 1.8f) - (Camera.main.transform.forward * 3);
        popup.Init($"+{msg.Amount}", 0.75f, 1f, 1f, playerInfo.Colour - new Color(0.2f, 0.2f, 0.2f, 0));

        _activePopups.Add(popup);

        if (msg.Reason == AwardCreditsReason.EnemyDefeated
            && Time.time >= _nextSoundTime)
        {
            AudioManager.PlayClipAtPoint(_enemyDefeatedAudio, msg.Position);
            _nextSoundTime = Time.time + SILENCE_BUFFER;
        }
    }

    private void OnSceneChange(SceneChangeData data)
    {
        switch (data.Mode)
        {
            case LoadSceneMode.Single: Flush(); break;
            case LoadSceneMode.Additive: SetAllInactive(); break;
        }
    }

    private void Flush()
    {
        foreach (Transform elem in transform)
        {
            Destroy(elem.gameObject);
        }

        _activePopups.Clear();
        _inactivePopups.Clear();
    }

    private void SetAllInactive()
    {
        for (int i = 0; i < _activePopups.Count; ++i)
        {
            var popup = _activePopups[i];
            popup.Hide();

            _inactivePopups.Push(popup);
        }

        _activePopups.Clear();
    }

    private void FixedUpdate()
    {
        for (int i = _activePopups.Count - 1; i >= 0; --i)
        {
            var popup = _activePopups[i];

            if (!popup.HasExpired)
            {
                continue;
            }

            popup.Hide(0.5f);

            _activePopups.RemoveAt(i);
            _inactivePopups.Push(popup);
        }
    }
}
