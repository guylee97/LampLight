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
		Map = LoadJson<MapData, string, MapPoint>("MapData");

		if (Map == null)
		{
			Debug.LogError("Failed to load Resources/Data/MapData.json");
			return;
		}

		MapPoints = Map.MakeDict();
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
