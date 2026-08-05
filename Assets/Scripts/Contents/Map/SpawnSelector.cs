using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSelector : MonoBehaviour
{
	public const string PlayerStartPoint = "player_start";

	[SerializeField]
	int _minPairDistance = 14;

	[SerializeField]
	int _maxAttempts = 64;

	[SerializeField]
	MapObjectPlacer _placer;

	MapPoint _playerStart;
	MapPoint _exitDoor;

	public Action<MapPoint, MapPoint> OnPairSelected;

	public MapPoint PlayerStart { get { return _playerStart; } }
	public MapPoint ExitDoor { get { return _exitDoor; } }
	public int MinPairDistance { get { return _minPairDistance; } }

	public static IReadOnlyList<MapPoint> Anchors
	{
		get
		{
			MapData map = Managers.Data.Map;
			return map == null ? new MapPoint[0] : map.spawns;
		}
	}

	public static bool TryPickPair(IReadOnlyList<MapPoint> anchors, int minDistance, int maxAttempts,
		System.Random rng, out MapPoint start, out MapPoint exit)
	{
		start = null;
		exit = null;

		if (anchors == null || anchors.Count < 2 || rng == null)
			return false;

		for (int attempt = 0; attempt < maxAttempts; attempt++)
		{
			int a = rng.Next(anchors.Count);
			int b = rng.Next(anchors.Count - 1);
			if (b >= a)
				b++;

			MapPoint candidateStart = anchors[a];
			MapPoint candidateExit = anchors[b];

			int distance = MapPathfinder.Distance(candidateStart, candidateExit);
			if (distance == MapPathfinder.Unreachable || distance < minDistance)
				continue;

			start = candidateStart;
			exit = candidateExit;
			return true;
		}

		return false;
	}

	public bool Select()
	{
		return Select(new System.Random());
	}

	public bool Select(int seed)
	{
		return Select(new System.Random(seed));
	}

	public bool Select(System.Random rng)
	{
		MapData map = Managers.Data.Map;
		MapPoint exit = map == null ? null : map.Find(MapObjectPlacer.ExitDoorPoint);

		if (exit == null)
		{
			Debug.LogError("SpawnSelector: 생성기가 만든 exit_door 가 없다");
			return false;
		}

		MapPoint start = PickStartFor(exit, rng);
		if (start == null)
		{
			Debug.LogError("SpawnSelector: 출구에서 충분히 떨어진 시작점을 못 찾았다");
			return false;
		}

		_playerStart = start;
		_exitDoor = exit;

		SyncMapPoints(start, exit);

		if (OnPairSelected != null)
			OnPairSelected.Invoke(_playerStart, _exitDoor);

		return true;
	}

	MapPoint PickStartFor(MapPoint exit, System.Random rng)
	{
		IReadOnlyList<MapPoint> anchors = Anchors;
		if (anchors == null || anchors.Count == 0)
			return null;

		List<MapPoint> ordered = new List<MapPoint>(anchors);

		for (int i = ordered.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			MapPoint tmp = ordered[i];
			ordered[i] = ordered[j];
			ordered[j] = tmp;
		}

		MapPoint farthest = null;
		int best = -1;

		foreach (MapPoint candidate in ordered)
		{
			int distance = MapPathfinder.Distance(candidate, exit);
			if (distance == MapPathfinder.Unreachable || BlockedByDecoration(candidate))
				continue;

			if (distance >= _minPairDistance)
				return candidate;

			if (distance > best)
			{
				best = distance;
				farthest = candidate;
			}
		}

		if (farthest != null)
			Debug.LogWarning($"SpawnSelector: 거리 {_minPairDistance} 이상인 시작점이 없어 "
				+ $"가장 먼 {best} 타일로 대체했다");

		return farthest;
	}

	public const float SpawnClearanceRadius = 0.35f;

	public static bool BlockedByDecoration(MapPoint point)
	{
		return point != null && BlockedByDecoration(MapCoord.ToWorld(point), SpawnClearanceRadius);
	}

	public static bool BlockedByDecoration(Vector3 world, float clearance)
	{
		MapData map = Managers.Data.Map;
		if (map == null || map.decorations == null)
			return false;

		foreach (MapDecoration deco in map.decorations)
		{
			if (deco.collisionEnabled == false
				|| deco.colliderWidth <= 0.0f || deco.colliderHeight <= 0.0f)
				continue;

			float centerX = deco.x + deco.width * 0.5f + deco.colliderOffsetX;
			float centerY = map.height - deco.y + deco.colliderOffsetY;

			if (Mathf.Abs(world.x - centerX) < deco.colliderWidth * 0.5f + clearance
				&& Mathf.Abs(world.y - centerY) < deco.colliderHeight * 0.5f + clearance)
				return true;
		}

		return false;
	}

	public static bool TryPickFarthestPair(IReadOnlyList<MapPoint> anchors,
		out MapPoint start, out MapPoint exit)
	{
		start = null;
		exit = null;

		if (anchors == null || anchors.Count < 2)
			return false;

		int best = -1;

		for (int i = 0; i < anchors.Count; i++)
		{
			for (int j = i + 1; j < anchors.Count; j++)
			{
				int distance = MapPathfinder.Distance(anchors[i], anchors[j]);
				if (distance == MapPathfinder.Unreachable || distance <= best)
					continue;

				best = distance;
				start = anchors[i];
				exit = anchors[j];
			}
		}

		return best > 0;
	}

	static void SyncMapPoints(MapPoint start, MapPoint exit)
	{
		MapData map = Managers.Data.Map;
		if (map == null || map.objects == null)
			return;

		foreach (MapPoint point in map.objects)
		{
			if (point.name == PlayerStartPoint)
				Copy(start, point);
			else if (point.name == MapObjectPlacer.ExitDoorPoint)
				Copy(exit, point);
		}

		Managers.Data.UseMap(map);
	}

	static void Copy(MapPoint from, MapPoint to)
	{
		to.col = from.col;
		to.row = from.row;
		to.x = from.x;
		to.y = from.y;
	}

	public Vector3 PlayerStartWorld()
	{
		return MapCoord.ToWorld(_playerStart);
	}

	public Vector3 ExitDoorWorld()
	{
		return MapCoord.ToWorld(_exitDoor);
	}
}
