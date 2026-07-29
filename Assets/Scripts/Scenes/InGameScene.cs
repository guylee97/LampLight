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
	int _seed = -1;

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

		_placer.Place();

		if (_selector.Select(_seed < 0 ? new System.Random() : new System.Random(_seed)))
			ApplySpawnPair();

		_hud = Managers.UI.ShowSceneUI<UI_InGame>();
		_hud.Setup(_progress, _player);

		Managers.Game.OnStageEnded += OnStageEnded;
	}

	void OnDestroy()
	{
		Managers.Game.OnStageEnded -= OnStageEnded;
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

		if (_progress != null && _placer != null && _selector != null && _player != null)
			return true;

		Debug.LogError("InGameScene: missing StageProgress, MapObjectPlacer, SpawnSelector or PlayerController");
		return false;
	}

	void ApplySpawnPair()
	{
		Vector3 start = _selector.PlayerStartWorld();
		_player.transform.position = start;

		if (_camera != null)
		{
			if (_camera.Target == null)
				_camera.Target = _player.transform;

			_camera.SnapToTarget();
		}

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

		UI_Result popup = Managers.UI.ShowPopupUI<UI_Result>();
		popup.Setup(result, _progress.Collected, _progress.Required);
	}
}
