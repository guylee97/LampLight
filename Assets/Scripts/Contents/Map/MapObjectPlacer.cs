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
	string _artifactPath = "Map/Artifact";

	[SerializeField]
	string _exitDoorPath = "Map/ExitDoor";

	readonly List<Artifact> _artifacts = new List<Artifact>();
	ExitDoor _exitDoor;

	public IReadOnlyList<Artifact> Artifacts { get { return _artifacts; } }
	public ExitDoor ExitDoor { get { return _exitDoor; } }

	void Start()
	{
		Place();
	}

	public void Place()
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

		foreach (MapPoint point in map.objects)
		{
			if (point.name.StartsWith(ArtifactPrefix))
				PlaceArtifact(point, parent);
			else if (point.name == ExitDoorPoint)
				PlaceExitDoor(point, parent);
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

		if (_exitDoor != null)
		{
			Managers.Resource.Destroy(_exitDoor.gameObject);
			_exitDoor = null;
		}
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
		GameObject go = Managers.Resource.Instantiate(_exitDoorPath, parent);
		if (go == null)
			return;

		go.transform.position = MapCoord.ToWorld(point);

		_exitDoor = go.GetComponent<ExitDoor>();
		if (_exitDoor == null)
		{
			Debug.LogError($"MapObjectPlacer: {_exitDoorPath} has no ExitDoor component");
			return;
		}

		_exitDoor.Init(_progress);
	}

	public void MoveExitDoor(MapPoint point)
	{
		if (_exitDoor == null || point == null)
			return;

		_exitDoor.transform.position = MapCoord.ToWorld(point);
	}
}
