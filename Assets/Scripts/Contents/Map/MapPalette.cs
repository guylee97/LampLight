using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapPalette
{
	public const string TileResourceDir = "Palette/Temple";

	static readonly Dictionary<int, TileBase> _cache = new Dictionary<int, TileBase>();

	public static void Invalidate()
	{
		_cache.Clear();
	}

	public static TileBase LoadByTileId(int tileId)
	{
		TileBase cached;
		if (_cache.TryGetValue(tileId, out cached))
			return cached;

		TileBase tile = Resources.Load<TileBase>($"{TileResourceDir}/{TempleManifest.TileAssetName(tileId)}");
		if (tile == null)
			Debug.LogWarning($"MapPalette: {TileResourceDir}/{TempleManifest.TileAssetName(tileId)} 없음");

		_cache[tileId] = tile;
		return tile;
	}

	public static Dictionary<int, TileBase> BuildLookup(MapData map)
	{
		Dictionary<int, TileBase> lookup = new Dictionary<int, TileBase>();
		if (map == null)
			return lookup;

		HashSet<int> gids = new HashSet<int>();
		Collect(map.floor, gids);
		Collect(map.walls, gids);
		Collect(map.deco, gids);

		foreach (int gid in gids)
		{
			TileBase tile = LoadByTileId(TempleManifest.GidToTileId(gid));
			if (tile != null)
				lookup[gid] = tile;
		}

		return lookup;
	}

	static void Collect(int[] layer, HashSet<int> into)
	{
		if (layer == null)
			return;

		foreach (int gid in layer)
		{
			if (gid != 0)
				into.Add(gid);
		}
	}
}
