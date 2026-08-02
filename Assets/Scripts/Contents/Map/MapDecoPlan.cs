using System.Collections.Generic;
using UnityEngine;

public struct DecoPlacement
{
	public string Key;
	public float TileX;
	public float TileY;
	public int SortRow;
}

public static class MapDecoPlan
{
	public const string CategoryWallDeco = "walldeco";
	public const string CategoryNoise = "noise";
	public const string CategoryContainer = "container";

	public static List<DecoPlacement> Build(MapData map, int seed)
	{
		return Build(map, seed, Managers.Game.CurrentLevel);
	}

	public static List<DecoPlacement> Build(MapData map, int seed, int level)
	{
		List<DecoPlacement> placed = new List<DecoPlacement>();

		if (map == null || map.rooms == null || TempleManifest.IsReady == false)
			return placed;

		System.Random rng = new System.Random(seed);
		HashSet<long> used = new HashSet<long>();

		ReserveObjectTiles(map, used);

		PlaceDebris(placed, used, map, rng);
		PlaceMoss(placed, used, map, rng);
		PlaceGlass(placed, used, map, rng, level);
		PlacePlanks(placed, used, map, rng, level);
		PlacePillars(placed, used, map, rng, level);
		PlaceSarcophagi(placed, used, map, rng, level);
		PlaceDrawers(placed, used, map, rng, level);
		PlaceWallPatterns(placed, used, map, rng, level);

		return placed;
	}

	static void ReserveObjectTiles(MapData map, HashSet<long> used)
	{
		if (map.objects == null)
			return;

		foreach (MapPoint point in map.objects)
		{
			for (int dr = -DecoSpec.ArtifactWallGap; dr <= DecoSpec.ArtifactWallGap; dr++)
			{
				for (int dc = -DecoSpec.ArtifactWallGap; dc <= DecoSpec.ArtifactWallGap; dc++)
					used.Add(Cell(point.col + dc, point.row + dr));
			}
		}
	}

	static void PlaceDebris(List<DecoPlacement> placed, HashSet<long> used, MapData map,
		System.Random rng)
	{
		List<string> pool = DecoSpec.Available(DecoSpec.DebrisKeys);
		if (pool.Count == 0)
			return;

		List<Vector2Int> walkable = WalkableTiles(map);
		int target = Mathf.RoundToInt(walkable.Count * DecoSpec.DebrisRatio);

		List<Vector2Int> nearWall = new List<Vector2Int>();
		List<Vector2Int> open = new List<Vector2Int>();

		foreach (Vector2Int tile in walkable)
		{
			if (TouchesWall(map, tile.x, tile.y))
				nearWall.Add(tile);
			else
				open.Add(tile);
		}

		Shuffle(nearWall, rng);
		Shuffle(open, rng);

		int fromWall = Mathf.RoundToInt(target * DecoSpec.DebrisWallWeight);
		Take(placed, used, nearWall, pool, fromWall, rng);
		Take(placed, used, open, pool, target - fromWall, rng);
	}

	static void PlaceMoss(List<DecoPlacement> placed, HashSet<long> used, MapData map,
		System.Random rng)
	{
		List<string> pool = DecoSpec.Available(DecoSpec.MossKeys);
		if (pool.Count == 0)
			return;

		List<Vector2Int> candidates = new List<Vector2Int>();
		int wallAdjacent = 0;

		foreach (Vector2Int tile in WalkableTiles(map))
		{
			int walls = WallSides(map, tile.x, tile.y);
			if (walls == 0)
				continue;

			wallAdjacent++;
			int weight = walls >= 2 ? DecoSpec.MossCornerWeight : 1;
			for (int i = 0; i < weight; i++)
				candidates.Add(tile);
		}

		if (candidates.Count == 0)
			return;

		Shuffle(candidates, rng);
		Take(placed, used, candidates, pool,
			Mathf.RoundToInt(wallAdjacent * DecoSpec.MossRatio), rng);
	}

	static void PlaceGlass(List<DecoPlacement> placed, HashSet<long> used, MapData map,
		System.Random rng, int level)
	{
		List<string> pool = DecoSpec.Available(DecoSpec.GlassKeys);
		if (pool.Count == 0)
			return;

		MapPoint start = map.Find("player_start");
		List<Vector2Int> spots = new List<Vector2Int>();

		foreach (Vector2Int tile in WalkableTiles(map))
		{
			if (InsideAnyRoom(map, tile.x, tile.y) && DoorwayTile(map, tile.x, tile.y) == false)
				continue;

			if (start != null && Chebyshev(tile.x, tile.y, start.col, start.row)
				< DecoSpec.GlassStartClearance)
				continue;

			spots.Add(tile);
		}

		Shuffle(spots, rng);

		List<Vector2Int> chosen = new List<Vector2Int>();
		int want = DecoSpec.GlassCount(level);

		foreach (Vector2Int tile in spots)
		{
			if (chosen.Count >= want)
				break;

			if (used.Contains(Cell(tile.x, tile.y)))
				continue;

			bool tooClose = false;
			foreach (Vector2Int other in chosen)
			{
				if (Chebyshev(tile.x, tile.y, other.x, other.y) < DecoSpec.GlassGapTiles)
				{
					tooClose = true;
					break;
				}
			}

			if (tooClose)
				continue;

			chosen.Add(tile);
			Add(placed, used, pool[rng.Next(pool.Count)], tile);
		}
	}

	static void PlacePlanks(List<DecoPlacement> placed, HashSet<long> used, MapData map,
		System.Random rng, int level)
	{
		if (DecoSpec.HasPlanks(level) == false)
			return;

		List<string> pool = DecoSpec.Available(DecoSpec.PlankKeys);
		if (pool.Count == 0)
			return;

		List<Vector2Int> borders = new List<Vector2Int>();

		foreach (Vector2Int tile in WalkableTiles(map))
		{
			if (DoorwayTile(map, tile.x, tile.y))
				borders.Add(tile);
		}

		Shuffle(borders, rng);
		Take(placed, used, borders, pool, DecoSpec.PlankCount(level), rng);
	}

	static void PlacePillars(List<DecoPlacement> placed, HashSet<long> used, MapData map,
		System.Random rng, int level)
	{
		if (DecoSpec.HasPillars(level) == false)
			return;

		List<string> intact = DecoSpec.Available(DecoSpec.PillarIntactKeys);
		List<string> broken = DecoSpec.Available(DecoSpec.PillarBrokenKeys);
		if (intact.Count == 0)
			return;

		foreach (MapRoom room in map.rooms)
		{
			if (room.Shorter < DecoSpec.PillarMinRoom)
				continue;

			int pairs = rng.Next(0, DecoSpec.PillarPairsPerRoomMax + 1);

			for (int i = 0; i < pairs; i++)
			{
				int left = room.col + DecoSpec.PillarWallGap;
				int right = room.Right - DecoSpec.PillarWallGap;
				if (right <= left)
					break;

				int span = Mathf.Max(1, room.height - DecoSpec.PillarWallGap * 2);
				int row = room.row + DecoSpec.PillarWallGap + rng.Next(0, span);

				if (Free(map, used, left, row) == false || Free(map, used, right, row) == false)
					continue;

				string whole = intact[rng.Next(intact.Count)];
				bool breakLeft = broken.Count > 0 && rng.NextDouble() < 0.5;

				Add(placed, used, breakLeft ? broken[rng.Next(broken.Count)] : whole,
					new Vector2Int(left, row));
				Add(placed, used, breakLeft ? whole : Pick(broken, whole, rng),
					new Vector2Int(right, row));
			}
		}
	}

	static void PlaceSarcophagi(List<DecoPlacement> placed, HashSet<long> used, MapData map,
		System.Random rng, int level)
	{
		if (DecoSpec.HasSarcophagus(level) == false)
			return;

		if (TempleManifest.Catalog.Object(DecoSpec.SarcophagusClosed) == null)
			return;

		int made = 0;

		foreach (MapRoom room in map.rooms)
		{
			if (made >= DecoSpec.SarcophagusPerMapMax)
				break;

			if (room.Shorter < DecoSpec.SarcophagusMinRoom)
				continue;

			List<Vector2Int> spots = new List<Vector2Int>();

			for (int row = room.row; row <= room.Bottom; row++)
			{
				for (int col = room.col; col <= room.Right; col++)
				{
					if (Free(map, used, col, row) && TouchesWall(map, col, row)
						&& DoorwayTile(map, col, row) == false
						&& HasPillarBeside(placed, col, row))
						spots.Add(new Vector2Int(col, row));
				}
			}

			if (spots.Count == 0)
				continue;

			Add(placed, used, DecoSpec.SarcophagusClosed, spots[rng.Next(spots.Count)]);
			made++;
		}
	}

	static void PlaceDrawers(List<DecoPlacement> placed, HashSet<long> used, MapData map,
		System.Random rng, int level)
	{
		if (DecoSpec.HasDrawer(level) == false)
			return;

		if (TempleManifest.Catalog.Object(DecoSpec.DrawerClosed) == null)
			return;

		foreach (MapRoom room in map.rooms)
		{
			int want = rng.Next(DecoSpec.DrawerPerRoomMin, DecoSpec.DrawerPerRoomMax + 1);
			List<Vector2Int> spots = new List<Vector2Int>();

			for (int row = room.row; row <= room.Bottom; row++)
			{
				for (int col = room.col; col <= room.Right; col++)
				{
					if (Free(map, used, col, row) && TouchesWall(map, col, row)
						&& DoorwayTile(map, col, row) == false
						&& RoomCenter(room, col, row) == false)
						spots.Add(new Vector2Int(col, row));
				}
			}

			Shuffle(spots, rng);
			for (int i = 0; i < want && i < spots.Count; i++)
				Add(placed, used, DecoSpec.DrawerClosed, spots[i]);
		}
	}

	static void PlaceWallPatterns(List<DecoPlacement> placed, HashSet<long> used, MapData map,
		System.Random rng, int level)
	{
		if (DecoSpec.HasWallPattern(level) == false)
			return;

		List<string> pool = DecoSpec.Available(DecoSpec.WallPatternKeys);
		if (pool.Count == 0)
			return;

		MapPoint exit = map.Find(MapObjectPlacer.ExitDoorPoint);
		int span = DecoSpec.WallPatternSpan;

		foreach (MapRoom room in map.rooms)
		{
			if (rng.NextDouble() >= 0.5)
				continue;

			List<int> spots = new List<int>();
			for (int col = room.col + 1; col < room.col + room.width - 1; col++)
			{
				if (WallFaceRules.BlockedBand(map, col, room.row, span, span) == false)
					continue;

				if (NearExit(exit, col, room.row))
					continue;

				spots.Add(col);
			}

			if (spots.Count == 0)
				continue;

			int chosen = spots[rng.Next(spots.Count)];

			placed.Add(new DecoPlacement
			{
				Key = pool[rng.Next(pool.Count)],
				TileX = chosen + 0.5f,
				TileY = room.row - 0.5f,
				SortRow = room.row - 1,
			});
		}
	}

	static bool NearExit(MapPoint exit, int col, int row)
	{
		if (exit == null)
			return false;

		return Mathf.Abs(exit.col - col) < DecoSpec.WallPatternExitGap
			&& Mathf.Abs(exit.row - row) < DecoSpec.WallPatternExitGap;
	}

	static bool HasPillarBeside(List<DecoPlacement> placed, int col, int row)
	{
		foreach (DecoPlacement placement in placed)
		{
			if (placement.Key.StartsWith("prop_pillar") == false)
				continue;

			if (Mathf.Abs(Mathf.FloorToInt(placement.TileY) - row) <= 1
				&& Mathf.Abs(Mathf.FloorToInt(placement.TileX) - col) <= 3)
				return true;
		}

		return false;
	}

	static string Pick(List<string> pool, string fallback, System.Random rng)
	{
		return pool.Count == 0 ? fallback : pool[rng.Next(pool.Count)];
	}

	static void Take(List<DecoPlacement> placed, HashSet<long> used, List<Vector2Int> spots,
		List<string> pool, int count, System.Random rng)
	{
		int made = 0;

		foreach (Vector2Int tile in spots)
		{
			if (made >= count)
				return;

			if (used.Contains(Cell(tile.x, tile.y)))
				continue;

			Add(placed, used, pool[rng.Next(pool.Count)], tile);
			made++;
		}
	}

	static void Add(List<DecoPlacement> placed, HashSet<long> used, string key, Vector2Int tile)
	{
		used.Add(Cell(tile.x, tile.y));

		placed.Add(new DecoPlacement
		{
			Key = key,
			TileX = tile.x + 0.5f,
			TileY = tile.y + 0.5f,
			SortRow = tile.y,
		});
	}

	static bool Free(MapData map, HashSet<long> used, int col, int row)
	{
		return map.Contains(col, row)
			&& map.GetGid(map.walls, col, row) == 0
			&& used.Contains(Cell(col, row)) == false;
	}

	static List<Vector2Int> WalkableTiles(MapData map)
	{
		List<Vector2Int> tiles = new List<Vector2Int>();

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (map.GetGid(map.walls, col, row) == 0)
					tiles.Add(new Vector2Int(col, row));
			}
		}

		return tiles;
	}

	static int WallSides(MapData map, int col, int row)
	{
		int count = 0;
		if (IsWall(map, col, row - 1)) count++;
		if (IsWall(map, col, row + 1)) count++;
		if (IsWall(map, col - 1, row)) count++;
		if (IsWall(map, col + 1, row)) count++;
		return count;
	}

	static bool TouchesWall(MapData map, int col, int row)
	{
		return WallSides(map, col, row) > 0;
	}

	static bool IsWall(MapData map, int col, int row)
	{
		return map.Contains(col, row) == false || map.GetGid(map.walls, col, row) != 0;
	}

	static bool InsideAnyRoom(MapData map, int col, int row)
	{
		if (map.rooms == null)
			return false;

		foreach (MapRoom room in map.rooms)
		{
			if (room.Contains(col, row))
				return true;
		}

		return false;
	}

	static bool DoorwayTile(MapData map, int col, int row)
	{
		bool inside = InsideAnyRoom(map, col, row);

		if (IsWall(map, col + 1, row) == false && InsideAnyRoom(map, col + 1, row) != inside)
			return true;

		if (IsWall(map, col - 1, row) == false && InsideAnyRoom(map, col - 1, row) != inside)
			return true;

		if (IsWall(map, col, row + 1) == false && InsideAnyRoom(map, col, row + 1) != inside)
			return true;

		if (IsWall(map, col, row - 1) == false && InsideAnyRoom(map, col, row - 1) != inside)
			return true;

		return false;
	}

	static bool RoomCenter(MapRoom room, int col, int row)
	{
		return Mathf.Abs(col - (room.col + room.width / 2)) <= 1
			&& Mathf.Abs(row - (room.row + room.height / 2)) <= 1;
	}

	static bool HasWallBand(MapData map, int col, int roomRow, int span)
	{
		return WallFaceRules.BlockedBand(map, col, roomRow, span, span);
	}

	static int Chebyshev(int ax, int ay, int bx, int by)
	{
		return Mathf.Max(Mathf.Abs(ax - bx), Mathf.Abs(ay - by));
	}

	static void Shuffle<T>(List<T> list, System.Random rng)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			T tmp = list[i];
			list[i] = list[j];
			list[j] = tmp;
		}
	}

	static long Cell(int col, int row)
	{
		return ((long)row << 32) ^ (uint)col;
	}
}
