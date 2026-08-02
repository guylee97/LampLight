using UnityEngine;

public static class MapRaycast
{
	public static int CountWalls(Vector3 from, Vector3 to)
	{
		if (MapCoord.IsReady == false)
			return 0;

		Vector2Int a = MapCoord.WorldToTile(from);
		Vector2Int b = MapCoord.WorldToTile(to);

		int dx = Mathf.Abs(b.x - a.x);
		int dy = Mathf.Abs(b.y - a.y);
		int sx = a.x < b.x ? 1 : -1;
		int sy = a.y < b.y ? 1 : -1;
		int err = dx - dy;

		int col = a.x;
		int row = a.y;
		int walls = 0;
		int guard = dx + dy + 2;

		while (guard-- > 0)
		{
			bool endpoint = (col == a.x && row == a.y) || (col == b.x && row == b.y);
			if (endpoint == false && MapCoord.IsWalkable(col, row) == false)
				walls++;

			if (col == b.x && row == b.y)
				break;

			int err2 = err * 2;
			if (err2 > -dy)
			{
				err -= dy;
				col += sx;
			}
			if (err2 < dx)
			{
				err += dx;
				row += sy;
			}
		}

		return walls;
	}
}
