using UnityEngine;

public static class MapCoord
{
	static MapData Map { get { return Managers.Data.Map; } }

	public static bool IsReady { get { return Map != null; } }

	public static int Width { get { return Map == null ? 0 : Map.width; } }
	public static int Height { get { return Map == null ? 0 : Map.height; } }

	public static Vector3 TileToWorld(int col, int row)
	{
		if (Map == null)
			return Vector3.zero;

		return new Vector3(col + 0.5f, (Map.height - 1 - row) + 0.5f, 0);
	}

	public static Vector2Int WorldToTile(Vector3 world)
	{
		if (Map == null)
			return Vector2Int.zero;

		int col = Mathf.FloorToInt(world.x);
		int row = Map.height - 1 - Mathf.FloorToInt(world.y);
		return new Vector2Int(col, row);
	}

	public static Vector3 ToWorld(MapPoint point)
	{
		if (point == null)
			return Vector3.zero;

		return new Vector3(point.x, point.y, 0);
	}

	public static Vector3 ToWorld(string pointName)
	{
		return ToWorld(Managers.Data.GetPoint(pointName));
	}

	public static bool Contains(int col, int row)
	{
		return Map != null && Map.Contains(col, row);
	}

	public static bool IsWalkable(int col, int row)
	{
		if (Map == null || Map.Contains(col, row) == false)
			return false;

		return Map.GetGid(Map.walls, col, row) == 0;
	}

	public static bool IsWalkable(Vector3 world)
	{
		Vector2Int tile = WorldToTile(world);
		return IsWalkable(tile.x, tile.y);
	}

	public static bool IsNoisy(int col, int row)
	{
		if (Map == null)
			return false;

		MapTileProp prop = Map.GetProp(Map.GetGid(Map.floor, col, row));
		return prop != null && prop.noisy;
	}

	public static bool IsNoisy(Vector3 world)
	{
		Vector2Int tile = WorldToTile(world);
		return IsNoisy(tile.x, tile.y);
	}

	public static Bounds WorldBounds()
	{
		if (Map == null)
			return new Bounds(Vector3.zero, Vector3.zero);

		Vector3 size = new Vector3(Map.width, Map.height, 0);
		return new Bounds(size * 0.5f, size);
	}
}
