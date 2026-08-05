using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
	const float EnemyClearance = 0.35f;

	[SerializeField]
	string _walkerPath = "WalkerZombie";

	[SerializeField]
	string _wandererPath = "WandererZombie";

	[SerializeField]
	string _runnerPath = "RunnerZombie";

	[SerializeField]
	Transform _root;

	[SerializeField]
	int _minDistanceFromStart = 7;

	[SerializeField]
	int _hookDistanceMin = 7;

	[SerializeField]
	int _hookDistanceMax = 9;

	readonly List<EnemyBase> _spawned = new List<EnemyBase>();

	public IReadOnlyList<EnemyBase> Spawned { get { return _spawned; } }

	public void Spawn(LevelConfig config, MapPoint start, System.Random rng)
	{
		ClearExisting();

		if (config == null || start == null || rng == null)
			return;

		int[] field = MapPathfinder.DistanceField(start.col, start.row);
		if (field == null)
			return;

		List<Vector2Int> far = new List<Vector2Int>();
		List<Vector2Int> hook = new List<Vector2Int>();
		CollectCandidates(field, far, hook);

		if (far.Count == 0)
			return;

		Transform parent = _root != null ? _root : transform;
		bool hookPlaced = false;

		for (int i = 0; i < config.WalkerCount; i++)
		{
			bool useHook = config.Level == LevelTable.MinLevel && hookPlaced == false && hook.Count > 0;
			List<Vector2Int> pool = useHook ? hook : far;
			SpawnOne(_walkerPath, pool[rng.Next(pool.Count)], parent);
			hookPlaced |= useHook;
		}

		for (int i = 0; i < config.WandererCount; i++)
			SpawnOne(_wandererPath, far[rng.Next(far.Count)], parent);

		for (int i = 0; i < config.RunnerCount; i++)
			SpawnOne(_runnerPath, far[rng.Next(far.Count)], parent);
	}

	void CollectCandidates(int[] field, List<Vector2Int> far, List<Vector2Int> hook)
	{
		MapData map = Managers.Data.Map;
		if (map == null)
			return;

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (MapCoord.IsWalkable(col, row) == false)
					continue;

				if (SpawnSelector.BlockedByDecoration(MapCoord.TileToWorld(col, row), EnemyClearance))
					continue;

				int distance = MapPathfinder.Sample(field, col, row);
				if (distance == MapPathfinder.Unreachable)
					continue;

				if (distance >= _minDistanceFromStart)
					far.Add(new Vector2Int(col, row));

				if (distance >= _hookDistanceMin && distance <= _hookDistanceMax)
					hook.Add(new Vector2Int(col, row));
			}
		}
	}

	void SpawnOne(string path, Vector2Int tile, Transform parent)
	{
		GameObject go = Managers.Resource.Instantiate(path, parent);
		if (go == null)
			return;

		go.transform.position = MapCoord.TileToWorld(tile.x, tile.y);

		EnemyBase enemy = go.GetComponent<EnemyBase>();
		if (enemy != null)
			_spawned.Add(enemy);
	}

	public void ClearExisting()
	{
		foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
		{
			if (enemy != null)
				Managers.Resource.Destroy(enemy.gameObject);
		}

		_spawned.Clear();
	}
}
