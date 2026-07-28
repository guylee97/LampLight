using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSelector : MonoBehaviour
{
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
		MapPoint start;
		MapPoint exit;

		if (TryPickPair(Anchors, _minPairDistance, _maxAttempts, rng, out start, out exit) == false)
		{
			Debug.LogError("SpawnSelector: failed to pick a valid spawn pair");
			return false;
		}

		_playerStart = start;
		_exitDoor = exit;

		if (_placer != null)
			_placer.MoveExitDoor(_exitDoor);

		if (OnPairSelected != null)
			OnPairSelected.Invoke(_playerStart, _exitDoor);

		return true;
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
