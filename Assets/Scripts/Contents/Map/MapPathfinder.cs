using System.Collections.Generic;
using UnityEngine;

public static class MapPathfinder
{
	public const int Unreachable = -1;

	static readonly int[] DirCol = { 1, -1, 0, 0 };
	static readonly int[] DirRow = { 0, 0, 1, -1 };

	public static int[] DistanceField(int startCol, int startRow)
	{
		MapData map = Managers.Data.Map;
		if (map == null)
			return null;

		int[] dist = new int[map.width * map.height];
		for (int i = 0; i < dist.Length; i++)
			dist[i] = Unreachable;

		if (MapCoord.IsWalkable(startCol, startRow) == false)
			return dist;

		Queue<int> queue = new Queue<int>();
		int startIndex = startRow * map.width + startCol;
		dist[startIndex] = 0;
		queue.Enqueue(startIndex);

		while (queue.Count > 0)
		{
			int index = queue.Dequeue();
			int col = index % map.width;
			int row = index / map.width;
			int next = dist[index] + 1;

			for (int d = 0; d < 4; d++)
			{
				int nc = col + DirCol[d];
				int nr = row + DirRow[d];

				if (MapCoord.IsWalkable(nc, nr) == false)
					continue;

				int ni = nr * map.width + nc;
				if (dist[ni] != Unreachable)
					continue;

				dist[ni] = next;
				queue.Enqueue(ni);
			}
		}

		return dist;
	}

	public static int[] DistanceField(Vector2Int start)
	{
		return DistanceField(start.x, start.y);
	}

	public static int Sample(int[] field, int col, int row)
	{
		MapData map = Managers.Data.Map;
		if (field == null || map == null || map.Contains(col, row) == false)
			return Unreachable;

		return field[row * map.width + col];
	}

	public static int Sample(int[] field, MapPoint point)
	{
		return point == null ? Unreachable : Sample(field, point.col, point.row);
	}

	public static int Distance(int fromCol, int fromRow, int toCol, int toRow)
	{
		return Sample(DistanceField(fromCol, fromRow), toCol, toRow);
	}

	public static int Distance(MapPoint from, MapPoint to)
	{
		if (from == null || to == null)
			return Unreachable;

		return Distance(from.col, from.row, to.col, to.row);
	}
}
