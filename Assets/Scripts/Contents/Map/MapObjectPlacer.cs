using System.Collections.Generic;
using UnityEngine;

public class MapObjectPlacer : MonoBehaviour
{
	public const string ArtifactPrefix = "artifact_";
	public const string ExitDoorPoint = "exit_door";

	[SerializeField]
	StageProgress _progress;

	[SerializeField]
	Transform _root;

	[SerializeField]
	Sprite[] _artifactSprites;

	[SerializeField]
	string _artifactPath = "Map/Artifact";

	[SerializeField]
	string _exitDoorPath = "Map/ExitDoor";

	readonly List<Artifact> _artifacts = new List<Artifact>();
	Altar _altar;
	Transform _parent;
	System.Random _rng;

	public IReadOnlyList<Artifact> Artifacts { get { return _artifacts; } }
	public Altar Altar { get { return _altar; } }

	public void Place()
	{
		Place(int.MaxValue, 0.0f);
	}

	public void Place(int maxArtifacts, float artifactRadiusTiles)
	{
		Place(maxArtifacts, artifactRadiusTiles, LevelTable.MinLevel);
	}

	public void Place(int maxArtifacts, float artifactRadiusTiles, int level)
	{
		Place(maxArtifacts, artifactRadiusTiles, level, Determinism.Stream(level));
	}

	public void Place(int maxArtifacts, float artifactRadiusTiles, int level, System.Random rng)
	{
		MapData map = Managers.Data.Map;
		if (map == null)
		{
			Debug.LogError("MapObjectPlacer: MapData is not loaded");
			return;
		}

		Clear();

		if (_progress != null)
			_progress.ResetProgress();

		Transform parent = _root != null ? _root : transform;
		_parent = parent;
		_rng = rng ?? Determinism.Stream(level);

		List<MapPoint> artifactPoints = new List<MapPoint>();
		foreach (MapPoint point in map.objects)
		{
			if (point.name.StartsWith(ArtifactPrefix) && artifactPoints.Count < maxArtifacts)
				artifactPoints.Add(point);
		}

		List<Vector2Int> candidates = CollectWalkableTiles(map);
		for (int i = 0; i < artifactPoints.Count && candidates.Count > 0; i++)
		{
			int index = _rng.Next(candidates.Count);
			Vector2Int tile = candidates[index];
			candidates.RemoveAt(index);
			PlaceArtifactAt(artifactPoints[i], tile, parent);
		}

		PlaceExitDoorFromMap(map, parent);

		ApplyConcealment(level);

		if (artifactRadiusTiles > 0.0f)
			ApplyArtifactRadius(artifactRadiusTiles);
	}

	void ApplyConcealment(int level)
	{
		for (int i = 0; i < _artifacts.Count; i++)
		{
			if (_artifacts[i] == null)
				continue;

			int concealment = ConcealmentRules.ForLevel(level, i);
			_artifacts[i].SetConcealment(concealment);

			Sprite sprite = LoadObjectSprite(DecoSpec.ArtifactKey(i, concealment));
			if (sprite == null && _artifactSprites != null && i < _artifactSprites.Length)
				sprite = _artifactSprites[i];

			if (sprite != null)
				_artifacts[i].SetSprite(sprite);
		}
	}

	public static Sprite LoadObjectSprite(string key)
	{
		if (string.IsNullOrEmpty(key) || TempleManifest.IsReady == false)
			return null;

		TempleObject entry = TempleManifest.Catalog.Object(key);
		return entry == null ? null : Resources.Load<Sprite>(entry.resource);
	}

	void ApplyArtifactRadius(float radiusTiles)
	{
		foreach (Artifact artifact in _artifacts)
		{
			if (artifact == null)
				continue;

			PingScheduler ping = artifact.GetComponent<PingScheduler>();
			if (ping != null)
				ping.RadiusTiles = radiusTiles * artifact.RadiusScale;
		}

		if (_altar != null)
		{
			PingScheduler ping = _altar.GetComponent<PingScheduler>();
			if (ping != null)
				ping.RadiusTiles = radiusTiles;
		}
	}

	public void Clear()
	{
		foreach (Artifact artifact in _artifacts)
		{
			if (artifact != null)
				Managers.Resource.Destroy(artifact.gameObject);
		}

		_artifacts.Clear();

		if (_altar != null)
		{
			Managers.Resource.Destroy(_altar.gameObject);
			_altar = null;
		}
	}

	void PlaceExitDoorFromMap(MapData map, Transform parent)
	{
		MapPoint point = map.Find(ExitDoorPoint);
		if (point == null)
		{
			Debug.LogError($"MapObjectPlacer: 맵에 {ExitDoorPoint} 지점이 없다");
			return;
		}

		PlaceExitDoor(point, parent);
	}

	static List<Vector2Int> CollectWalkableTiles(MapData map)
	{
		List<Vector2Int> candidates = new List<Vector2Int>();
		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (MapCoord.IsPassable(col, row))
					candidates.Add(new Vector2Int(col, row));
			}
		}

		return candidates;
	}

	void PlaceArtifact(MapPoint point, Transform parent)
	{
		GameObject go = Managers.Resource.Instantiate(_artifactPath, parent);
		if (go == null)
			return;

		go.transform.position = MapCoord.ToWorld(point);

		Artifact artifact = go.GetComponent<Artifact>();
		if (artifact == null)
		{
			Debug.LogError($"MapObjectPlacer: {_artifactPath} has no Artifact component");
			return;
		}

		artifact.Init(_progress, point.name);
		_artifacts.Add(artifact);
	}

	void PlaceArtifactAt(MapPoint source, Vector2Int tile, Transform parent)
	{
		MapPoint randomized = new MapPoint
		{
			name = source.name,
			col = tile.x,
			row = tile.y,
			x = tile.x + 0.5f,
			y = Managers.Data.Map.height - tile.y - 0.5f,
		};
		PlaceArtifact(randomized, parent);
	}

	void PlaceExitDoor(MapPoint point, Transform parent)
	{
		GameObject go = Managers.Resource.Instantiate(_exitDoorPath, parent);
		if (go == null)
			return;

		go.transform.position = MapCoord.ToWorld(point);

		ExitDoor legacy = go.GetComponent<ExitDoor>();
		if (legacy != null)
			Destroy(legacy);

		_altar = go.GetComponent<Altar>();
		if (_altar == null)
			_altar = go.AddComponent<Altar>();

		_altar.UseCatalogSprite();
		_altar.Init(_progress);
	}
}
