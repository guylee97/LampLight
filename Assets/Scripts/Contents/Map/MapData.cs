using System;
using System.Collections.Generic;

[Serializable]
public class MapTileProp
{
	public int gid;
	public bool walkable;
	public bool noisy;
}

[Serializable]
public class MapPoint
{
	public string name;
	public int col;
	public int row;
	public float x;
	public float y;
}

[Serializable]
public class MapData : ILoader<string, MapPoint>
{
	public int width;
	public int height;
	public int tileSize;
	public int pixelWidth;
	public int pixelHeight;

	public int[] floor;
	public int[] walls;
	public int[] deco;

	public MapTileProp[] tileProps;
	public MapPoint[] objects;
	public MapPoint[] spawns;

	Dictionary<int, MapTileProp> _propByGid;

	public Dictionary<string, MapPoint> MakeDict()
	{
		Dictionary<string, MapPoint> dict = new Dictionary<string, MapPoint>();

		foreach (MapPoint point in objects)
			dict[point.name] = point;

		foreach (MapPoint point in spawns)
			dict[point.name] = point;

		return dict;
	}

	public bool Contains(int col, int row)
	{
		return col >= 0 && col < width && row >= 0 && row < height;
	}

	public int GetGid(int[] layer, int col, int row)
	{
		if (layer == null || Contains(col, row) == false)
			return 0;

		return layer[row * width + col];
	}

	public MapTileProp GetProp(int gid)
	{
		if (gid == 0)
			return null;

		if (_propByGid == null)
		{
			_propByGid = new Dictionary<int, MapTileProp>();
			if (tileProps != null)
			{
				foreach (MapTileProp prop in tileProps)
					_propByGid[prop.gid] = prop;
			}
		}

		MapTileProp found;
		return _propByGid.TryGetValue(gid, out found) ? found : null;
	}
}
