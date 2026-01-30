using HoverTanks.Events;
using HoverTanks.GameState;
using HoverTanks.Networking;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Game
{
	/// <summary>
	/// Thread-safe Time.time. Thanks Unity.
	/// </summary>
	public static float time { get; set; }

	public static bool isPauseMenuOpen { get; set; }
}

public class GameManager : MonoBehaviour
{
	public const TeamId DefaultAITeamId = TeamId.Beta;

	public static int fixedFrameCount { get; private set; }
	public static bool IsUsingController { get; private set; }
	public static GameManager instance;

	public Texture DefaultTexture;
	public AnimationCurve TurnDecelCurve;
	public LayerMask GeometryLayer;
	public LayerMask FloorLayer;
	public LayerMask BounceLayer;
	public LayerMask ExplosionLayer;
	public EffectAudioSettings DamageAudio;
	public EffectAudioSettings HealAudio;
	public EffectAudioSettings GlancingBlowAudio;
	public EffectAudioSettings SteamAudio;
	public EffectAudioSettings HeatCriticalAudio;

	private AsyncOperation _asyncLoad;

	#if UNITY_EDITOR
	[SerializeField] ScreenshotCamera _screenshotCamera;
	#endif

	public static void Run(IEnumerator routine)
	{
        instance.StartCoroutine(routine);
	}

	private void Awake()
	{
		if (instance == null)
		{
			InitSingleton();
		}
		else
		{
			Destroy(this.gameObject);
		}
	}

	private void Update()
	{
		Game.time = Time.time;
		ThreadManager.Update();

#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.F12))
		{
			_screenshotCamera.Snap();
		}
#endif
	}

	private void InitSingleton()
	{
		instance = this;

		SceneManager.sceneLoaded += SceneLoaded;
		//Application.targetFrameRate = 60;

		// network events
		NetworkEvents.Subscribe<ServerShuttingDownMsg>(OnServerShuttingDownMsg);
		NetworkEvents.Subscribe<LoadSceneMsg>(OnLoadSceneMsg);

		// local events
		LocalEvents.Subscribe<DisconnectedData>(OnDisconnected);

		DontDestroyOnLoad(this.gameObject);
	}

    private void FixedUpdate()
	{
		++fixedFrameCount;
	}

	public static void LoadScene(int index)
	{
		PrepareForSceneChange();
		SceneManager.LoadScene(index);
	}

	public static void LoadScene(string name)
	{
		PrepareForSceneChange();
		SceneManager.LoadScene(name);
	}

	public static void ReloadScene()
	{
		var scene = SceneManager.GetActiveScene();
		LoadScene(scene.buildIndex);
	}

	public static AsyncOperation LoadSceneAsyncAdditive(string name)
	{
		PrepareForSceneChange();
		return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
	}

	public static AsyncOperation UnloadSceneAsync(string name)
	{
		return SceneManager.UnloadSceneAsync(name);
	}

	private void SceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (instance != this)
		{
			return;
		}

		// reset timescale
		Time.timeScale = 1;

		// notify listeners
		LocalEvents.Invoke(new SceneChangeData()
		{
			SceneName = scene.name,
			Mode = mode,
		});

		switch (mode)
		{
			case LoadSceneMode.Single:
			{
				DynamicDecals.Flush();
			} break;

			case LoadSceneMode.Additive:
			{
				_asyncLoad = null;
			} break;
		}

		ActivateStateInScene();
	}

	private void ActivateStateInScene()
	{
		var state = FindFirstObjectByType<GameState>();

		if (state != null)
		{
			state.TriggerState();
		}
		else
		{
			Log.Warning(LogChannel.GameManager, "SceneLoaded - No GameState found in scene.");
		}
	}

	private void OnServerShuttingDownMsg(ServerShuttingDownMsg msg)
	{
		HandleSelfDisconnect();
	}

	private void OnLoadSceneMsg(LoadSceneMsg msg)
    {
		if (msg.Mode == LoadSceneMode.Single)
		{
			LoadScene(msg.SceneName);
		}
		else if (msg.Mode == LoadSceneMode.Additive)
		{
			if (_asyncLoad == null)
			{
				_asyncLoad = LoadSceneAsyncAdditive(msg.SceneName);
			}
			else
			{
				Log.Error(LogChannel.GameManager, $"OnLoadSceneMsg - already running async load");
			}
		}
    }

	private void OnDisconnected(DisconnectedData data)
	{
		HandleSelfDisconnect();
	}

	private void HandleSelfDisconnect()
	{
        string sceneName = SceneManager.GetActiveScene().name;
		string menuScene = "GameStateMenu";

		if (sceneName != menuScene)
		{
			LoadScene(menuScene);
		}
	}

	private static void PrepareForSceneChange()
	{
		DebrisManager.Flush();
		AudioManager.StopAllSFX();
	}
}
