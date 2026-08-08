using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InGameScene : MonoBehaviour
{
	const string DistantCueClip = "Monster/평상시/capaholiczsfx-creature-growl-deep-bass-403153";
	const string DistantCueFallback = "zombie_idle_3";
	const float DistantCueSeconds = 9.0f;
	const float SpawnGraceSeconds = 6.0f;

	// 요괴는 처음부터 있지 않다. 신전은 조용하고, 공양물을 건드려서 깨어난다.
	// 한 박자 두고 나타나야 원인과 결과로 읽힌다 — 집는 즉시 나오면 우연처럼 보인다.
	const float WakeAfterOfferingSeconds = 3.0f;

	// 아무것도 줍지 않아도 언젠가는 깨어난다. 빈 신전을 끝까지 걷게 두지 않는다.
	const float WakeCeilingShare = 0.25f;
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

		_placer.Place(
			config.ArtifactsPlaced,
			config.ArtifactRadiusTiles,
			config.Level);

		if (_placer.Altar != null)
			_placer.Altar.SetChannelSeconds(config.RitualSeconds);

		if (_selector.Select())
			PlacePlayer();

		_hud = Managers.UI.ShowSceneUI<UI_InGame>();
		_hud.Setup(_progress, _player);

		Managers.Game.OnStageEnded += OnStageEnded;
		_progress.OnArtifactCollected += OnArtifactCollected;
		MaskYokai.OnLostInDark += OnLostInDark;

		OpeningLines(config);

		StartCoroutine(DistantCue());
		StartCoroutine(WakeYokai(config, configured));
	}

	/// 정적 → 기척 → 사냥. 등불이 2.9타일만 밝히니 먼 목격은 성립하지 않는다.
	/// 등장은 소리로 알리고, 눈으로 보는 순간은 등불 안에 들어올 때다.
	IEnumerator WakeYokai(LevelConfig config, int seed)
	{
		float ceiling = config.LampSeconds * WakeCeilingShare;
		float started = Time.time;

		while (_progress.Collected == 0 && Time.time - started < ceiling)
			yield return null;

		if (_progress.Collected > 0)
			yield return new WaitForSeconds(WakeAfterOfferingSeconds);

		// 먼 곳의 기척(9초)이 먼저 깔린 뒤에 깨어나야 순서가 맞는다.
		while (Time.time - started < DistantCueSeconds + 1.0f)
			yield return null;

		Wake(config, seed);
	}

	void Wake(LevelConfig config, int seed)
	{
		if (_spawner == null)
		{
			PushEnemiesAwayFrom(_selector.PlayerStart);
			return;
		}

		// 시작점이 아니라 지금 서 있는 자리에서 거리를 잰다.
		Vector2Int tile = MapCoord.WorldToTile(_player.transform.position);
		MapPoint here = new MapPoint { name = "player_now", col = tile.x, row = tile.y };

		_spawner.Spawn(config, here, new System.Random(seed < 0 ? Determinism.Seed : seed));

		foreach (EnemyBase enemy in _spawner.Spawned)
		{
			MaskYokai yokai = enemy as MaskYokai;
			if (yokai == null)
				continue;

			yokai.HoldSensesFor(SpawnGraceSeconds);

			// 어디서 깨어났는지 소리로만 알린다. 그 자리는 등불 밖이라 보이지 않는다.
			Managers.Sound.PlayAtPointOptional(
				DistantCueClip, DistantCueFallback,
				yokai.transform.position, Define.Sound.Threat, 0.85f);
		}
	}

	IEnumerator DistantCue()
	{
		yield return new WaitForSeconds(DistantCueSeconds);

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
			"눈을 떠보니 버려진 신전이야. 등불 하나 남았고.",
			"제단이 비어 있어. 이 불 꺼지기 전에 채워야겠지.");
	}

	void OnArtifactCollected(int collected, int required)
	{
		if (_player != null)
			FallingDust.Burst(_player.transform.position + Vector3.up * 1.4f, 7, 1.1f, collected * 977);

		string count = collected >= required ? "다 모았어. 제단으로." : "하나 찾았어. 아직 모자라.";
		string sense = OfferingLine(collected);

		if (sense == null)
			UI_Dialogue.Say(count);
		else
			UI_Dialogue.Say(count, sense);
	}

	static string OfferingLine(int collected)
	{
		YokaiSpec spec = YokaiTable.ForLevel(Managers.Game.CurrentLevel);

		if (spec == null || spec.OfferingLines == null || spec.OfferingLines.Length == 0)
			return null;

		int index = Mathf.Clamp(collected - 1, 0, spec.OfferingLines.Length - 1);
		return spec.OfferingLines[index];
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

	void PlacePlayer()
	{
		_player.Teleport(_selector.PlayerStartWorld());

		if (_camera == null)
			return;

		if (_camera.Target == null)
			_camera.Target = _player.transform;

		_camera.SnapToTarget();
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
