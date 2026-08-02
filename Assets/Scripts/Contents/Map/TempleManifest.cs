using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TempleTile
{
	public int id;
	public string name;
	public bool walkable;
	public bool noisy;
}

[Serializable]
public class TempleObject
{
	public string key;
	public string file;
	public string resource;
	public string category;
	public string footprint;
	public int w;
	public int h;

	public int Cols { get { return FootprintPart(0); } }
	public int Rows { get { return FootprintPart(1); } }

	int FootprintPart(int index)
	{
		if (string.IsNullOrEmpty(footprint))
			return 1;

		string[] parts = footprint.Split('x');
		if (parts.Length != 2)
			return 1;

		int value;
		return int.TryParse(parts[index], out value) ? Mathf.Max(1, value) : 1;
	}
}

[Serializable]
public class TempleCatalog
{
	public int tilePx;
	public int displayScale;
	public int columns;
	public int autotileBase;
	public int maskNorth;
	public int maskEast;
	public int maskSouth;
	public int maskWest;

	public TempleTile[] tiles;
	public TempleObject[] objects;

	Dictionary<int, TempleTile> _tileById;
	Dictionary<string, TempleObject> _objectByKey;

	public TempleTile Tile(int tileId)
	{
		if (_tileById == null)
		{
			_tileById = new Dictionary<int, TempleTile>();
			if (tiles != null)
			{
				foreach (TempleTile tile in tiles)
					_tileById[tile.id] = tile;
			}
		}

		TempleTile found;
		return _tileById.TryGetValue(tileId, out found) ? found : null;
	}

	public TempleObject Object(string key)
	{
		if (_objectByKey == null)
		{
			_objectByKey = new Dictionary<string, TempleObject>();
			if (objects != null)
			{
				foreach (TempleObject entry in objects)
					_objectByKey[entry.key] = entry;
			}
		}

		TempleObject found;
		return _objectByKey.TryGetValue(key, out found) ? found : null;
	}

	public List<TempleObject> ByCategory(string category)
	{
		List<TempleObject> matched = new List<TempleObject>();
		if (objects == null)
			return matched;

		foreach (TempleObject entry in objects)
		{
			if (entry.category == category)
				matched.Add(entry);
		}

		return matched;
	}
}

public static class TempleManifest
{
	public const string CatalogResource = "Data/temple_catalog";
	public const string TileAssetPrefix = "tile_";

	static TempleCatalog _catalog;

	public static TempleCatalog Catalog
	{
		get
		{
			if (_catalog == null)
				Load();

			return _catalog;
		}
	}

	public static bool IsReady { get { return Catalog != null; } }

	public static void Load()
	{
		TextAsset text = Resources.Load<TextAsset>(CatalogResource);
		if (text == null)
		{
			Debug.LogError($"TempleManifest: Resources/{CatalogResource}.json 없음. Tools/manifest_to_unity.py 를 돌려라");
			return;
		}

		_catalog = JsonUtility.FromJson<TempleCatalog>(text.text);

		if (_catalog == null || _catalog.tiles == null || _catalog.tiles.Length == 0)
			Debug.LogError("TempleManifest: temple_catalog.json 파싱 실패");
	}

	public static void Invalidate()
	{
		_catalog = null;
	}

	public static string TileAssetName(int tileId)
	{
		return $"{TileAssetPrefix}{tileId:D2}";
	}

	public static int TileIdToGid(int tileId)
	{
		return tileId + 1;
	}

	public static int GidToTileId(int gid)
	{
		return gid - 1;
	}

	public static int WallMask(bool north, bool east, bool south, bool west)
	{
		TempleCatalog catalog = Catalog;
		if (catalog == null)
			return 0;

		int mask = 0;
		if (north) mask |= catalog.maskNorth;
		if (east) mask |= catalog.maskEast;
		if (south) mask |= catalog.maskSouth;
		if (west) mask |= catalog.maskWest;

		return mask;
	}

	public static int WallTileId(bool north, bool east, bool south, bool west)
	{
		TempleCatalog catalog = Catalog;
		if (catalog == null)
			return 1;

		return catalog.autotileBase + WallMask(north, east, south, west);
	}

	public static int FloorTileId(bool noisy)
	{
		TempleCatalog catalog = Catalog;
		if (catalog == null)
			return 0;

		foreach (TempleTile tile in catalog.tiles)
		{
			if (tile.walkable && tile.noisy == noisy)
				return tile.id;
		}

		return 0;
	}

	public static List<int> FloorTileIds(bool noisy)
	{
		List<int> ids = new List<int>();
		TempleCatalog catalog = Catalog;
		if (catalog == null)
			return ids;

		foreach (TempleTile tile in catalog.tiles)
		{
			if (tile.walkable && tile.noisy == noisy)
				ids.Add(tile.id);
		}

		return ids;
	}
}
