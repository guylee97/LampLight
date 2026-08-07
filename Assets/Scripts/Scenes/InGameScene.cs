using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InGameScene : MonoBehaviour
{
	const string DistantCueClip = "Monster/평상시/capaholiczsfx-creature-growl-deep-bass-403153";
	const string DistantCueFallback = "zombie_idle_3";
	const float DistantCueSeconds = 9.0f;
	const float SpawnGraceSeconds = 3.5f;
	const float ArrivalCameoDelay = 1.6f;
	const int DistantCueMinTiles = 12;
	const int DistantCueMaxTiles = 20;

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
	LevelConfig _pendingSpawnConfig;
	System.Random _pendingSpawnRng;
	bool _saidLostInDark;

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

	void Start()
	{
		if (ResolveReferences() == false)
			return;

		Managers.Game.BeginStage();
		Managers.Game.SetPlayer(_player.gameObject);
		Altar.ResetProgress();
		HorrorMix.ResetState();
		ChaseTension.Ensure();

		LevelConfig config = Managers.Game.Level;

		int configured = SeedOverride >= 0 ? SeedOverride : _seed;
		int mapSeed = configured >= 0
			? configured
			: Determinism.Seed;
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

		System.Random rng = new System.Random(configured < 0 ? Determinism.Seed : configured);
		int containedArtifacts = ConfigureArtifactContainers(config);
		_placer.Place(
			config.ArtifactsPlaced - containedArtifacts,
			config.ArtifactRadiusTiles,
			config.Level,
			rng);

		if (_placer.Altar != null)
			_placer.Altar.SetChannelSeconds(config.RitualSeconds);

		if (_selector.Select())
			ApplySpawnPair(config, rng);

		_hud = Managers.UI.ShowSceneUI<UI_InGame>();
		_hud.Setup(_progress, _player);

		Managers.Game.OnStageEnded += OnStageEnded;
		_progress.OnArtifactCollected += OnArtifactCollected;
		MaskYokai.OnLostInDark += OnLostInDark;

		OpeningLines(config);

		if (config.Level > LevelTable.MinLevel)
			StartCoroutine(ArrivalCameo(config));

		StartCoroutine(DistantCue());
	}

	IEnumerator ArrivalCameo(LevelConfig config)
	{
		yield return new WaitForSeconds(ArrivalCameoDelay);

		if (_player != null)
			YokaiCameo.Play(YokaiTable.ForLevel(config.Level), _player.transform);
	}

	IEnumerator DistantCue()
	{
		yield return new WaitForSeconds(DistantCueSeconds);

		if (_pendingSpawnConfig == null)
			yield break;

		Vector3 where;
		if (TryFarPoint(out where) == false)
			yield break;

		Managers.Sound.PlayAtPointOptional(
			DistantCueClip, DistantCueFallback, where, Define.Sound.Threat, 0.7f);
	}

	bool TryFarPoint(out Vector3 world)
	{
		world = Vector3.zero;

		MapPoint start = _selector != null ? _selector.PlayerStart : null;
		if (start == null)
			return false;

		int[] field = MapPathfinder.DistanceField(start.col, start.row);
		MapData map = Managers.Data.Map;

		if (field == null || map == null)
			return false;

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				int distance = MapPathfinder.Sample(field, col, row);

				if (distance >= DistantCueMinTiles && distance <= DistantCueMaxTiles)
				{
					world = MapCoord.TileToWorld(col, row);
					return true;
				}
			}
		}

		return false;
	}

	void OnLostInDark()
	{
		if (_saidLostInDark)
			return;

		_saidLostInDark = true;
		UI_Dialogue.Say("그냥 지나갔다.");
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

		if (_player != null && _player.Lamp != null)
		{
			float seconds = config.LampSeconds;

			if (Managers.Game.ConsecutiveFailures >= 3)
				seconds *= 1.2f;

			_player.Lamp.SetMaxDuration(seconds);
			_player.Lamp.OnBurnedOut += OnLampBurnedOut;
		}
	}

	void OnLampBurnedOut()
	{
		if (Managers.Game.IsPlaying)
			Managers.Game.GameOver();
	}

	void OpeningLines(LevelConfig config)
	{
		UI_Dialogue.Clear();

		if (config.Level > LevelTable.MinLevel)
		{
			UI_Dialogue.Say(
				"안쪽으로 더 들어왔어.",
				"아까 그게 끝이 아니었나 봐.");
			return;
		}

		UI_Dialogue.Say(
			"눈을 떠보니 버려진 절이야. 등불 하나 남았고.",
			"제단이 비어 있어. 이 불 꺼지기 전에 채워야겠지.");
	}

	void OnArtifactCollected(int collected, int required)
	{
		SpawnYokai();

		if (_player != null)
			FallingDust.Burst(_player.transform.position + Vector3.up * 1.4f, 7, 1.1f, collected * 977);

		if (collected >= required)
			UI_Dialogue.Say("다 모았다. 제단으로.");
		else
			UI_Dialogue.Say("하나 찾았다. 아직 모자라.");
	}

	void OnDestroy()
	{
		if (_progress != null)
			_progress.OnArtifactCollected -= OnArtifactCollected;

		MaskYokai.OnLostInDark -= OnLostInDark;

		if (_player != null && _player.Lamp != null)
			_player.Lamp.OnBurnedOut -= OnLampBurnedOut;

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
		{
			_pendingSpawnConfig = config;
			_pendingSpawnRng = rng;
		}
		else
		{
			PushEnemiesAwayFrom(_selector.PlayerStart);
		}
	}

	void SpawnYokai()
	{
		if (_pendingSpawnConfig == null || _spawner == null || _selector == null)
			return;

		LevelConfig config = _pendingSpawnConfig;
		_spawner.Spawn(config, _selector.PlayerStart, _pendingSpawnRng);
		_pendingSpawnConfig = null;
		_pendingSpawnRng = null;

		foreach (EnemyBase enemy in _spawner.Spawned)
		{
			MaskYokai yokai = enemy as MaskYokai;
			if (yokai != null)
				yokai.HoldSensesFor(SpawnGraceSeconds);
		}

		if (_player != null)
			YokaiCameo.Play(YokaiTable.ForLevel(config.Level), _player.transform);
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

		System.Random rng = Determinism.Stream(_seed < 0 ? 1 : _seed + 1);

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

		containers[Managers.Game.CurrentLevel % containers.Length].SetArtifact(_progress);
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
