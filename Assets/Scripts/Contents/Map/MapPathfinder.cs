using System.Collections.Generic;
using UnityEngine;

public static class MapPathfinder
{
	public const int Unreachable = -1;

	static readonly int[] DirCol = { 1, -1, 0, 0 };
	static readonly int[] DirRow = { 0, 0, 1, -1 };

	static readonly int[] StepCol = { 1, -1, 0, 0, 1, 1, -1, -1 };
	static readonly int[] StepRow = { 0, 0, 1, -1, 1, -1, 1, -1 };
	static readonly int[] StepCost = { 10, 10, 10, 10, 14, 14, 14, 14 };

	static readonly Dictionary<int, int[]> _fieldCache = new Dictionary<int, int[]>();
	static int _cacheWidth;
	static int _cacheHeight;

	static int[] _gScore;
	static int[] _fScore;
	static int[] _cameFrom;
	static byte[] _closed;
	static int[] _openHeap;
	static int _openCount;
	static int _searchWidth;
	static int _searchHeight;

	public static void InvalidateCache()
	{
		_fieldCache.Clear();
		_cacheWidth = 0;
		_cacheHeight = 0;
		_gScore = null;
	}

	static bool EnsureCache()
	{
		MapData map = Managers.Data.Map;
		if (map == null)
			return false;

		if (map.width != _cacheWidth || map.height != _cacheHeight)
		{
			_fieldCache.Clear();
			_cacheWidth = map.width;
			_cacheHeight = map.height;
		}

		return true;
	}

	public static int[] DistanceField(int startCol, int startRow)
	{
		if (EnsureCache() == false)
			return null;

		MapData map = Managers.Data.Map;

		if (map.Contains(startCol, startRow) == false)
			return NewField(map);

		int key = startRow * map.width + startCol;

		int[] cached;
		if (_fieldCache.TryGetValue(key, out cached))
			return cached;

		int[] dist = NewField(map);

		if (MapCoord.IsPassable(startCol, startRow) == false)
		{
			_fieldCache[key] = dist;
			return dist;
		}

		Queue<int> queue = new Queue<int>();
		dist[key] = 0;
		queue.Enqueue(key);

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

				if (MapCoord.IsPassable(nc, nr) == false)
					continue;

				int ni = nr * map.width + nc;
				if (dist[ni] != Unreachable)
					continue;

				dist[ni] = next;
				queue.Enqueue(ni);
			}
		}

		_fieldCache[key] = dist;
		return dist;
	}

	static int[] NewField(MapData map)
	{
		int[] dist = new int[map.width * map.height];
		for (int i = 0; i < dist.Length; i++)
			dist[i] = Unreachable;

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

	public static bool TryFindPath(Vector2Int start, Vector2Int goal, List<Vector2Int> path)
	{
		if (path == null)
			return false;

		path.Clear();

		MapData map = Managers.Data.Map;
		if (map == null)
			return false;

		if (MapCoord.IsPassable(start.x, start.y) == false || MapCoord.IsPassable(goal.x, goal.y) == false)
			return false;

		int width = map.width;
		int height = map.height;
		int cells = width * height;

		if (_gScore == null || _searchWidth != width || _searchHeight != height)
		{
			_gScore = new int[cells];
			_fScore = new int[cells];
			_cameFrom = new int[cells];
			_closed = new byte[cells];
			_openHeap = new int[cells * 8 + 1];
			_searchWidth = width;
			_searchHeight = height;
		}

		for (int i = 0; i < cells; i++)
		{
			_gScore[i] = int.MaxValue;
			_cameFrom[i] = -1;
			_closed[i] = 0;
		}

		_openCount = 0;

		int startIndex = start.y * width + start.x;
		int goalIndex = goal.y * width + goal.x;

		_gScore[startIndex] = 0;
		_fScore[startIndex] = Heuristic(start.x, start.y, goal.x, goal.y);
		HeapPush(startIndex);

		while (_openCount > 0)
		{
			int current = HeapPop();

			if (current == goalIndex)
				return Reconstruct(current, startIndex, width, path);

			if (_closed[current] != 0)
				continue;

			_closed[current] = 1;

			int col = current % width;
			int row = current / width;

			for (int d = 0; d < 8; d++)
			{
				int nc = col + StepCol[d];
				int nr = row + StepRow[d];

				if (MapCoord.IsPassable(nc, nr) == false)
					continue;

				if (d >= 4 && (MapCoord.IsPassable(nc, row) == false || MapCoord.IsPassable(col, nr) == false))
					continue;

				int ni = nr * width + nc;
				if (_closed[ni] != 0)
					continue;

				int tentative = _gScore[current] + StepCost[d];
				if (tentative >= _gScore[ni])
					continue;

				_gScore[ni] = tentative;
				_cameFrom[ni] = current;
				_fScore[ni] = tentative + Heuristic(nc, nr, goal.x, goal.y);
				HeapPush(ni);
			}
		}

		return false;
	}

	static int Heuristic(int col, int row, int goalCol, int goalRow)
	{
		int dx = Mathf.Abs(col - goalCol);
		int dy = Mathf.Abs(row - goalRow);
		int min = Mathf.Min(dx, dy);
		return 14 * min + 10 * (dx + dy - 2 * min);
	}

	static bool Reconstruct(int current, int startIndex, int width, List<Vector2Int> path)
	{
		while (current != -1)
		{
			path.Add(new Vector2Int(current % width, current / width));
			if (current == startIndex)
				break;

			current = _cameFrom[current];
		}

		path.Reverse();
		return path.Count > 0;
	}

	static void HeapPush(int index)
	{
		if (_openCount + 1 >= _openHeap.Length)
			return;

		_openCount++;
		_openHeap[_openCount] = index;

		int child = _openCount;
		while (child > 1)
		{
			int parent = child / 2;
			if (_fScore[_openHeap[parent]] <= _fScore[_openHeap[child]])
				break;

			int swap = _openHeap[parent];
			_openHeap[parent] = _openHeap[child];
			_openHeap[child] = swap;
			child = parent;
		}
	}

	static int HeapPop()
	{
		int top = _openHeap[1];
		_openHeap[1] = _openHeap[_openCount];
		_openCount--;

		int parent = 1;
		while (true)
		{
			int left = parent * 2;
			int right = left + 1;
			int best = parent;

			if (left <= _openCount && _fScore[_openHeap[left]] < _fScore[_openHeap[best]])
				best = left;

			if (right <= _openCount && _fScore[_openHeap[right]] < _fScore[_openHeap[best]])
				best = right;

			if (best == parent)
				break;

			int swap = _openHeap[parent];
			_openHeap[parent] = _openHeap[best];
			_openHeap[best] = swap;
			parent = best;
		}

		return top;
	}
}
