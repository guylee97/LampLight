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
public class MapTileset
{
	public int firstGid;
	public int count;
	public string name;

	public bool Owns(int gid)
	{
		return gid >= firstGid && gid < firstGid + count;
	}
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
	public MapTileset[] tilesets;
	public MapPoint[] objects;
	public MapPoint[] spawns;

	Dictionary<int, MapTileProp> _propByGid;

	public Dictionary<string, MapPoint> MakeDict()
	{
		Dictionary<string, MapPoint> dict = new Dictionary<string, MapPoint>();

		if (objects != null)
		{
			foreach (MapPoint point in objects)
				dict[point.name] = point;
		}

		if (spawns != null)
		{
			foreach (MapPoint point in spawns)
				dict[point.name] = point;
		}

		return dict;
	}

	public bool Validate(out string error)
	{
		error = null;

		if (width <= 0 || height <= 0)
		{
			error = $"invalid size {width}x{height}";
			return false;
		}

		if (tileSize <= 0)
		{
			error = $"invalid tileSize {tileSize}";
			return false;
		}

		int expected = width * height;
		if (LayerLength(floor) != expected || LayerLength(walls) != expected || LayerLength(deco) != expected)
		{
			error = $"layer length mismatch: expected {expected}, got floor={LayerLength(floor)} walls={LayerLength(walls)} deco={LayerLength(deco)}";
			return false;
		}

		if (spawns == null || spawns.Length == 0)
		{
			error = "no spawn anchors";
			return false;
		}

		if (objects == null || objects.Length == 0)
		{
			error = "no map objects";
			return false;
		}

		return true;
	}

	static int LayerLength(int[] layer)
	{
		return layer == null ? -1 : layer.Length;
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

	public string GetTilesetName(int gid)
	{
		if (gid == 0 || tilesets == null)
			return null;

		foreach (MapTileset tileset in tilesets)
		{
			if (tileset.Owns(gid))
				return tileset.name;
		}

		return null;
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
