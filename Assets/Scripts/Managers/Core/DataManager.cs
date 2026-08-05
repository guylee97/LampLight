using System.Collections.Generic;
using UnityEngine;

public interface ILoader<Key, Value>
{
	Dictionary<Key, Value> MakeDict();
}

public class DataManager
{
	public MapData Map { get; private set; }
	public Dictionary<string, MapPoint> MapPoints { get; private set; } = new Dictionary<string, MapPoint>();

	public void Init()
	{
		MapPoints = new Dictionary<string, MapPoint>();
		MapPathfinder.InvalidateCache();
		Map = LoadJson<MapData, string, MapPoint>("MapData");

		if (Map == null)
		{
			Debug.LogError("Failed to load Resources/Data/MapData.json");
			return;
		}

		string error;
		if (Map.Validate(out error) == false)
		{
			Debug.LogError($"Resources/Data/MapData.json is malformed : {error}");
			Map = null;
			return;
		}

		MapPoints = Map.MakeDict();
	}

	public void UseMap(MapData map)
	{
		MapPathfinder.InvalidateCache();
		Map = map;
		MapPoints = map == null ? new Dictionary<string, MapPoint>() : map.MakeDict();
	}

	public int LastSeed { get; private set; } = -1;
	public bool LastUsedFallback { get; private set; }

	public bool BuildLevelMap(int level, int seed)
	{
		LastSeed = -1;
		LastUsedFallback = false;
		return LoadLevelMap(level);
	}

	public bool LoadLevelMap(int level)
	{
		string path = $"map_l{LevelTable.Clamp(level)}";
		MapData map = LoadJson<MapData, string, MapPoint>(path);

		string error;
		if (map == null || map.Validate(out error) == false)
		{
			Debug.LogError($"DataManager: {path} unusable, keeping current map");
			return false;
		}

		UseMap(map);
		return true;
	}

	public MapPoint GetPoint(string name)
	{
		MapPoint found;
		return MapPoints.TryGetValue(name, out found) ? found : null;
	}

	Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
	{
		TextAsset textAsset = Managers.Resource.Load<TextAsset>($"Data/{path}");
		if (textAsset == null)
		{
			Debug.LogError($"Failed to load TextAsset : Data/{path}");
			return default(Loader);
		}

		return JsonUtility.FromJson<Loader>(textAsset.text);
	}
}
