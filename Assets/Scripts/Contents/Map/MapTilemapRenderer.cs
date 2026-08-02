using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapTilemapRenderer : MonoBehaviour
{
	[SerializeField]
	Tilemap _floor;

	[SerializeField]
	Tilemap _wall;

	[SerializeField]
	Tilemap _deco;

	public bool HasTilemaps { get { return _floor != null && _wall != null; } }

	public void Repaint()
	{
		Repaint(Managers.Data.Map);
	}

	public void Repaint(MapData map)
	{
		Resolve();

		if (map == null || HasTilemaps == false)
			return;

		Dictionary<int, TileBase> tiles = MapPalette.BuildLookup(map);

		Paint(_floor, map, map.floor, tiles);
		Paint(_wall, map, map.walls, tiles);
		Paint(_deco, map, map.deco, tiles);

		RebuildColliders(_wall);
	}

	public static void RebuildColliders(Tilemap tilemap)
	{
		if (tilemap == null)
			return;

		TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
		if (collider == null)
		{
			Debug.LogError($"MapTilemapRenderer: {tilemap.name} 에 TilemapCollider2D 가 없다");
			return;
		}

		collider.enabled = false;
		collider.enabled = true;
		collider.ProcessTilemapChanges();

		CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();
		if (composite != null)
			composite.GenerateGeometry();
	}

	void Resolve()
	{
		if (_floor != null && _wall != null && _deco != null)
			return;

		foreach (Tilemap tilemap in FindObjectsByType<Tilemap>(FindObjectsInactive.Include,
			FindObjectsSortMode.None))
		{
			if (_floor == null && tilemap.name == "Floor")
				_floor = tilemap;
			else if (_wall == null && tilemap.name == "Wall")
				_wall = tilemap;
			else if (_deco == null && tilemap.name == "Decoration")
				_deco = tilemap;
		}
	}

	static void Paint(Tilemap tilemap, MapData map, int[] layer, Dictionary<int, TileBase> tiles)
	{
		if (tilemap == null)
			return;

		tilemap.ClearAllTiles();

		if (layer == null)
			return;

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				int gid = layer[row * map.width + col];
				if (gid == 0)
					continue;

				TileBase tile;
				if (tiles.TryGetValue(gid, out tile) == false || tile == null)
					continue;

				tilemap.SetTile(new Vector3Int(col, map.height - 1 - row, 0), tile);
			}
		}

		tilemap.RefreshAllTiles();
	}
}
