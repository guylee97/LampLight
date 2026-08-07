using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InGameScene : MonoBehaviour
{
	[SerializeField]
	StageProgress _progress;

	[SerializeField]
	MapObjectPlacer _placer;

	[SerializeField]
	SpawnSelector _selector;

	[SerializeField]
	PlayerController _player;

	[SerializeField]
	CameraController _camera;

	[SerializeField]
	EnemySpawner _spawner;

	[SerializeField]
	MapTilemapRenderer _tilemap;

	[SerializeField]
	MapDecoPlacer _deco;

	[SerializeField]
	int _seed = -1;

	public static int SeedOverride = -1;

	[SerializeField]
	int _minEnemyDistanceFromStart = 12;

	UI_InGame _hud;
	bool _escapeWasPressed;
	float _remainingSeconds;

	void Start()
	{
		if (ResolveReferences() == false)
			return;

		Managers.Game.BeginStage();
		Managers.Game.SetPlayer(_player.gameObject);
		Altar.ResetProgress();
		HorrorMix.ResetState();

		LevelConfig config = Managers.Game.Level;

		int configured = SeedOverride >= 0 ? SeedOverride : _seed;
		int mapSeed = configured >= 0
			? configured
			: UnityEngine.Random.Range(1, int.MaxValue - MapGenerator.MaxAttempts);
		Managers.Data.BuildLevelMap(config.Level, mapSeed);

		if (_tilemap == null)
			_tilemap = FindFirstObjectByType<MapTilemapRenderer>();

		if (_tilemap != null)
			_tilemap.Repaint();

		if (_deco == null)
			_deco = FindFirstObjectByType<MapDecoPlacer>();

		if (_deco != null)
			_deco.Place(Managers.Data.LastSeed >= 0 ? Managers.Data.LastSeed : mapSeed);

		BakeCollision();

		ApplyLevelConfig(config);

		System.Random rng = configured < 0 ? new System.Random() : new System.Random(configured);
		int containedArtifacts = ConfigureArtifactContainers(config);
		_placer.Place(
			config.ArtifactsPlaced - containedArtifacts,
			config.ArtifactRadiusTiles,
			config.Level,
			rng);

		if (_placer.Altar != null)
			_placer.Altar.SetChannelSeconds(config.RitualSeconds);

		if (_selector.Select(rng))
			ApplySpawnPair(config, rng);

		_hud = Managers.UI.ShowSceneUI<UI_InGame>();
		_hud.Setup(_progress, _player, config.DeadlineSeconds);
		_remainingSeconds = config.DeadlineSeconds;

		Managers.Game.OnStageEnded += OnStageEnded;
	}

	void BakeCollision()
	{
		MapCollisionBaker baker = FindFirstObjectByType<MapCollisionBaker>();
		if (baker == null)
		{
			GameObject host = new GameObject(nameof(MapCollisionBaker));
			baker = host.AddComponent<MapCollisionBaker>();
		}

		baker.Build(Managers.Data.Map);
	}

	void ApplyLevelConfig(LevelConfig config)
	{
		_progress.SetRequired(config.ArtifactsRequired);
		ApplyOilCanisters(config.OilCanisters);
		NoiseLure.ClearAll();

		if (_player != null)
		{
			StoneThrower thrower = _player.GetComponent<StoneThrower>();
			if (thrower != null)
				thrower.SetStones(config.Stones);
		}

		if (_player != null && _player.Lamp != null)
		{
			float seconds = config.LampSeconds;

			if (Managers.Game.ConsecutiveFailures >= 3)
				seconds *= 1.2f;

			_player.Lamp.SetMaxDuration(seconds);
		}
	}

	void OnDestroy()
	{
		if (Managers.TryGetGame(out GameManagerEx game))
			game.OnStageEnded -= OnStageEnded;
	}

	bool ResolveReferences()
	{
		if (_progress == null)
			_progress = FindFirstObjectByType<StageProgress>();

		if (_placer == null)
			_placer = FindFirstObjectByType<MapObjectPlacer>();

		if (_selector == null)
			_selector = FindFirstObjectByType<SpawnSelector>();

		if (_player == null)
			_player = FindFirstObjectByType<PlayerController>();

		if (_camera == null)
			_camera = FindFirstObjectByType<CameraController>();

		if (_spawner == null)
			_spawner = FindFirstObjectByType<EnemySpawner>();

		if (_progress != null && _placer != null && _selector != null && _player != null)
			return true;

		Debug.LogError("InGameScene: missing StageProgress, MapObjectPlacer, SpawnSelector or PlayerController");
		return false;
	}

	void ApplyOilCanisters(int allowed)
	{
		int kept = 0;

		foreach (OilCanister canister in FindObjectsByType<OilCanister>(FindObjectsInactive.Include,
			FindObjectsSortMode.InstanceID))
		{
			bool enable = kept < allowed;
			canister.gameObject.SetActive(enable);

			if (enable)
				kept++;
		}
	}

	void ApplySpawnPair(LevelConfig config, System.Random rng)
	{
		Vector3 start = _selector.PlayerStartWorld();
		_player.Teleport(start);

		if (_camera != null)
		{
			if (_camera.Target == null)
				_camera.Target = _player.transform;

			_camera.SnapToTarget();
		}

		if (_spawner != null)
			_spawner.Spawn(config, _selector.PlayerStart, rng);
		else
			PushEnemiesAwayFrom(_selector.PlayerStart);
	}

	void PushEnemiesAwayFrom(MapPoint start)
	{
		if (start == null)
			return;

		int[] field = MapPathfinder.DistanceField(start.col, start.row);
		if (field == null)
			return;

		List<Vector2Int> candidates = new List<Vector2Int>();
		MapData map = Managers.Data.Map;

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				int distance = MapPathfinder.Sample(field, col, row);
				if (distance != MapPathfinder.Unreachable && distance >= _minEnemyDistanceFromStart)
					candidates.Add(new Vector2Int(col, row));
			}
		}

		if (candidates.Count == 0)
			return;

		System.Random rng = _seed < 0 ? new System.Random() : new System.Random(_seed + 1);

		foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
		{
			Vector2Int tile = MapCoord.WorldToTile(enemy.transform.position);
			int distance = MapPathfinder.Sample(field, tile.x, tile.y);

			if (distance != MapPathfinder.Unreachable && distance >= _minEnemyDistanceFromStart)
				continue;

			Vector2Int moved = candidates[rng.Next(candidates.Count)];
			enemy.transform.position = MapCoord.TileToWorld(moved.x, moved.y);
		}
	}

	void Update()
	{
		if (Managers.Game.IsPlaying)
		{
			_remainingSeconds = Mathf.Max(0.0f, _remainingSeconds - Time.deltaTime);
			if (_hud != null)
				_hud.SetRemainingTime(_remainingSeconds);
			if (_remainingSeconds <= 0.0f)
				Managers.Game.GameOver();
		}

		Keyboard keyboard = Keyboard.current;
		if (keyboard == null)
			return;

		bool pressed = keyboard.escapeKey.isPressed;
		if (pressed && _escapeWasPressed == false)
			TogglePause();

		_escapeWasPressed = pressed;
	}

	int ConfigureArtifactContainers(LevelConfig config)
	{
		Container[] containers = FindObjectsByType<Container>(FindObjectsSortMode.InstanceID);
		if (containers.Length == 0 || config.ArtifactsPlaced <= 1)
			return 0;

		containers[UnityEngine.Random.Range(0, containers.Length)].SetArtifact(_progress);
		return 1;
	}

	void TogglePause()
	{
		if (Managers.Game.Result != Define.StageResult.None)
			return;

		Managers.Game.TogglePause();

		if (Managers.Game.IsPaused)
			Managers.UI.ShowPopupUI<UI_Pause>();
		else
			Managers.UI.CloseAllPopupUI();
	}

	void OnStageEnded(Define.StageResult result)
	{
		if (result != Define.StageResult.Cleared)
			return;

		if (Managers.Game.HasNextLevel)
		{
			Managers.Game.AdvanceLevel();
			Managers.Scene.LoadScene(Define.Scene.InGame);
			return;
		}

		UI_Result popup = Managers.UI.ShowPopupUI<UI_Result>();
		popup.Setup(result, _progress.Collected, _progress.Required);
	}
}
