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

	public const float ActorHalfWidth = 0.31f;
	public const float ActorHalfHeight = 0.16f;
	public const float ActorFootOffset = -0.317f;

	static MapData _blockedFor;
	static bool[] _blockedTiles;

	public static bool IsPassable(int col, int row)
	{
		if (IsWalkable(col, row) == false)
			return false;

		bool[] blocked = BlockedTiles();
		return blocked == null || blocked[row * Map.width + col] == false;
	}

	public static void InvalidateBlockedTiles()
	{
		_blockedFor = null;
		_blockedTiles = null;
	}

	static bool[] BlockedTiles()
	{
		MapData map = Map;
		if (map == null)
			return null;

		if (ReferenceEquals(_blockedFor, map))
			return _blockedTiles;

		_blockedFor = map;
		_blockedTiles = BuildBlockedTiles(map);
		return _blockedTiles;
	}

	static bool[] BuildBlockedTiles(MapData map)
	{
		bool[] blocked = new bool[map.width * map.height];
		if (map.decorations == null)
			return blocked;

		foreach (MapDecoration deco in map.decorations)
		{
			if (deco.collisionEnabled == false
				|| deco.colliderWidth <= 0.0f || deco.colliderHeight <= 0.0f)
				continue;

			if (MapDecoPlacer.SealsMapObject(deco))
				continue;

			float centerX = deco.x + deco.width * 0.5f + deco.colliderOffsetX;
			float centerY = map.height - deco.y + deco.colliderOffsetY;
			float halfWidth = deco.colliderWidth * 0.5f + ActorHalfWidth;
			float halfHeight = deco.colliderHeight * 0.5f + ActorHalfHeight;

			int minCol = Mathf.FloorToInt(centerX - halfWidth - 0.5f);
			int maxCol = Mathf.CeilToInt(centerX + halfWidth - 0.5f);
			int minRow = Mathf.FloorToInt(
				map.height - 1 - (centerY + halfHeight - 0.5f - ActorFootOffset)) - 1;
			int maxRow = Mathf.CeilToInt(
				map.height - 1 - (centerY - halfHeight - 0.5f - ActorFootOffset)) + 1;

			for (int row = minRow; row <= maxRow; row++)
			{
				for (int col = minCol; col <= maxCol; col++)
				{
					if (map.Contains(col, row) == false)
						continue;

					float tileX = col + 0.5f;
					float tileY = map.height - 1 - row + 0.5f + ActorFootOffset;

					if (Mathf.Abs(tileX - centerX) < halfWidth
						&& Mathf.Abs(tileY - centerY) < halfHeight)
						blocked[row * map.width + col] = true;
				}
			}
		}

		return blocked;
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
