using UnityEngine;

public static class WallFaceRules
{
	public const int DoorCols = 3;
	public const int DoorRows = 3;

	public delegate bool WalkableAt(int col, int row);

	public static bool IsDoorway(WalkableAt walkable, int col, int row, int cols, int rows)
	{
		int half = cols / 2;

		for (int dx = -half; dx <= half; dx++)
		{
			if (walkable(col + dx, row) == false)
				return false;
		}

		for (int depth = 1; depth <= rows; depth++)
		{
			for (int dx = -half; dx <= half; dx++)
			{
				if (walkable(col + dx, row - depth))
					return false;
			}
		}

		return true;
	}

	public static bool IsDoorway(MapData map, int col, int row, int cols, int rows)
	{
		if (InBounds(map, col, row, cols, rows) == false)
			return false;

		return IsDoorway(delegate (int c, int r) { return Walkable(map, c, r); }, col, row, cols, rows);
	}

	public static bool InBounds(MapData map, int col, int row, int cols, int rows)
	{
		int half = cols / 2;
		return map != null && map.Contains(col - half, row) && map.Contains(col + half, row)
			&& map.Contains(col, row - rows);
	}

	public static bool Walkable(MapData map, int col, int row)
	{
		return map != null && map.Contains(col, row) && map.GetGid(map.walls, col, row) == 0;
	}

	public static bool Blocked(MapData map, int col, int row)
	{
		return Walkable(map, col, row) == false;
	}

	public static bool BlockedBand(MapData map, int col, int row, int cols, int rows)
	{
		if (InBounds(map, col, row, cols, rows) == false)
			return false;

		int half = cols / 2;

		for (int depth = 1; depth <= rows; depth++)
		{
			for (int dx = -half; dx <= half; dx++)
			{
				if (Blocked(map, col + dx, row - depth) == false)
					return false;
			}
		}

		return true;
	}

	public static Vector2 BaseOf(MapData map, int col, int row)
	{
		return new Vector2(col + 0.5f, map.height - row);
	}
}
