using System;
using System.Collections.Generic;
using UnityEngine;

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
public class MapRoom
{
	public int col;
	public int row;
	public int width;
	public int height;

	public int Right { get { return col + width - 1; } }
	public int Bottom { get { return row + height - 1; } }
	public int Shorter { get { return Mathf.Min(width, height); } }

	public bool Contains(int c, int r)
	{
		return c >= col && c <= Right && r >= row && r <= Bottom;
	}

	public bool Fits(int cols, int rows)
	{
		return width >= cols && height >= rows;
	}
}

[Serializable]
public class MapDecoration
{
	public string key;
	public string resource;
	public float x;
	public float y;
	public float width;
	public float height;
	public bool flipHorizontal;
	public bool flipVertical;
	public bool flipDiagonal;
	public bool collisionEnabled;
	public float colliderWidth;
	public float colliderHeight;
	public float colliderOffsetX;
	public float colliderOffsetY;
	public int sortingOffset;
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
	public int[] collision;

	public MapTileProp[] tileProps;
	public MapTileset[] tilesets;
	public MapPoint[] objects;
	public MapPoint[] spawns;
	public MapRoom[] rooms;
	public MapDecoration[] decorations;

	[NonSerialized]
	public int[] blocked;

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

		if (collision != null)
		{
			if (collision.Length != expected)
			{
				error = $"collision length mismatch: expected {expected}, got {collision.Length}";
				return false;
			}

			for (int i = 0; i < collision.Length; i++)
			{
				int code = collision[i];
				if (code != MapCoord.CollisionWalk && code != MapCoord.CollisionBlock
					&& code != MapCoord.CollisionNoise && code != MapCoord.CollisionMuffled)
				{
					error = $"collision[{i}] has undefined code {code}";
					return false;
				}
			}
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

	public MapPoint Find(string pointName)
	{
		if (objects != null)
		{
			foreach (MapPoint point in objects)
			{
				if (point.name == pointName)
					return point;
			}
		}

		if (spawns == null)
			return null;

		foreach (MapPoint point in spawns)
		{
			if (point.name == pointName)
				return point;
		}

		return null;
	}

	public void ClearBlocked()
	{
		blocked = null;
	}

	public void Block(int col, int row)
	{
		if (Contains(col, row) == false)
			return;

		if (blocked == null)
			blocked = new int[width * height];

		blocked[row * width + col] = 1;
	}

	public bool IsBlocked(int col, int row)
	{
		if (blocked == null || Contains(col, row) == false)
			return false;

		return blocked[row * width + col] != 0;
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
