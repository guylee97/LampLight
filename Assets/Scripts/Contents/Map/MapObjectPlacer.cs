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
	string _altarPath = "Map/ExitDoor";

	readonly List<Artifact> _artifacts = new List<Artifact>();
	Altar _altar;
	Transform _parent;

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

		// 맵을 구울 때 정한 자리를 그대로 쓴다. 굽는 쪽이 제단과의 거리, 공양물 사이
		// 간격, 소품에 가리지 않을 것을 전부 보장해 두는데, 여기서 다시 뽑으면
		// 그 보장이 전부 버려지고 테스트가 재는 맵과 실제로 노는 맵이 갈라진다.
		int placed = 0;
		foreach (MapPoint point in map.objects)
		{
			if (point.name.StartsWith(ArtifactPrefix) == false)
				continue;

			if (placed >= maxArtifacts)
				break;

			PlaceArtifact(point, parent);
			placed++;
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

			Sprite exposed = LoadObjectSprite(DecoSpec.ArtifactKey(i, 0));
			_artifacts[i].SetMarkSprite(exposed != null ? exposed : sprite);

			if (_altar != null)
			{
				Artifact carried = _artifacts[i];
				carried.OnCollected += a => _altar.Carry(a.MarkSprite);
			}
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

	void PlaceExitDoor(MapPoint point, Transform parent)
	{
		GameObject go = Managers.Resource.Instantiate(_altarPath, parent);
		if (go == null)
			return;

		go.transform.position = MapCoord.ToWorld(point);

		_altar = go.GetComponent<Altar>();
		if (_altar == null)
			_altar = go.AddComponent<Altar>();

		_altar.UseCatalogSprite();
		_altar.Init(_progress);
	}
}
