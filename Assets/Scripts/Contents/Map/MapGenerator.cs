using System;
using System.Collections.Generic;
using UnityEngine;

public static class MapGenerator
{
	public const int LeafMin = 7;
	public const int CorridorWidth = 2;
	public const int RoomMin = 4;
	public const int RoomMax = 14;
	public const int MaxAttempts = 200;
	public const float MinWalkRatio = 0.35f;
	public const float MaxWalkRatio = 0.52f;

	public const string TempleTilesetName = "temple";
	public const int SpawnCount = 8;

	const float FloorVariantChance = 0.18f;
	const float NoisyFloorChance = 0.10f;

	struct Rect
	{
		public int X, Y, W, H;

		public Rect(int x, int y, int w, int h)
		{
			X = x; Y = y; W = w; H = h;
		}

		public Vector2Int Center { get { return new Vector2Int(X + W / 2, Y + H / 2); } }
	}

	public class Spec
	{
		public int Level;
		public int Width;
		public int Height;
		public int MinRooms;
		public int MaxRooms;
		public int Artifacts;
		public float SoundRadius;
		public bool Noisy;
		public bool HookNearStart;

		public static Spec For(int level)
		{
			return For(level, null);
		}

		public static Spec For(int level, System.Random rng)
		{
			LevelConfig config = LevelTable.Get(level);
			SizeRange range = SizeRange.For(level);

			int width = range.RollWidth(rng);
			int height = range.RollHeight(rng);

			switch (LevelTable.Clamp(level))
			{
				case 1:
					return new Spec { Level = 1, Width = width, Height = height,
						MinRooms = 4, MaxRooms = 10,
						Artifacts = config.ArtifactsPlaced, SoundRadius = config.ArtifactRadiusTiles,
						Noisy = false, HookNearStart = true };

				case 2:
					return new Spec { Level = 2, Width = width, Height = height,
						MinRooms = 6, MaxRooms = 14,
						Artifacts = config.ArtifactsPlaced, SoundRadius = config.ArtifactRadiusTiles,
						Noisy = true, HookNearStart = false };

				default:
					return new Spec { Level = 3, Width = width, Height = height,
						MinRooms = 8, MaxRooms = 18,
						Artifacts = config.ArtifactsPlaced, SoundRadius = config.ArtifactRadiusTiles,
						Noisy = true, HookNearStart = false };
			}
		}
	}

	public struct SizeRange
	{
		public int MinWidth;
		public int TargetWidth;
		public int MaxWidth;
		public int MinHeight;
		public int TargetHeight;
		public int MaxHeight;

		public static SizeRange For(int level)
		{
			switch (LevelTable.Clamp(level))
			{
				case 1:
					return new SizeRange { MinWidth = 26, TargetWidth = 28, MaxWidth = 30,
						MinHeight = 17, TargetHeight = 19, MaxHeight = 21 };

				case 2:
					return new SizeRange { MinWidth = 32, TargetWidth = 35, MaxWidth = 38,
						MinHeight = 21, TargetHeight = 23, MaxHeight = 25 };

				default:
					return new SizeRange { MinWidth = 40, TargetWidth = 43, MaxWidth = 46,
						MinHeight = 25, TargetHeight = 27, MaxHeight = 29 };
			}
		}

		public int RollWidth(System.Random rng)
		{
			return rng == null ? TargetWidth : rng.Next(MinWidth, MaxWidth + 1);
		}

		public int RollHeight(System.Random rng)
		{
			return rng == null ? TargetHeight : rng.Next(MinHeight, MaxHeight + 1);
		}
	}

	static int Candidate(int seed, int attempt)
	{
		if (attempt == 0)
			return seed;

		unchecked
		{
			uint h = (uint)seed ^ 0x9E3779B9u;
			h ^= (uint)attempt * 0x85EBCA6Bu;
			h ^= h >> 15;
			h *= 0xC2B2AE35u;
			h ^= h >> 13;
			return (int)(h & 0x7FFFFFFF);
		}
	}

	public static MapData Generate(int level, int seed, out int usedSeed)
	{
		for (int attempt = 0; attempt < MaxAttempts; attempt++)
		{
			usedSeed = Candidate(seed, attempt);

			Spec spec = Spec.For(level, new System.Random(usedSeed ^ 0x5F3A7C1));
			MapData map = TryBuild(spec, usedSeed);
			if (map != null)
				return map;
		}

		usedSeed = -1;
		return null;
	}

	static MapData TryBuild(Spec spec, int seed)
	{
		System.Random rng = new System.Random(seed);

		List<Rect> leaves = new List<Rect>();
		Split(new Rect(0, 0, spec.Width, spec.Height), rng, leaves);

		List<Rect> rooms = new List<Rect>();
		foreach (Rect leaf in leaves)
		{
			Rect room;
			if (TryCarve(leaf, rng, out room))
				rooms.Add(room);
		}

		if (rooms.Count < spec.MinRooms || rooms.Count > spec.MaxRooms)
			return null;

		bool[,] walk = new bool[spec.Height, spec.Width];
		foreach (Rect room in rooms)
		{
			for (int y = room.Y; y < room.Y + room.H; y++)
			{
				for (int x = room.X; x < room.X + room.W; x++)
					walk[y, x] = true;
			}
		}

		rooms.Sort((a, b) => a.Center.x != b.Center.x
			? a.Center.x.CompareTo(b.Center.x)
			: a.Center.y.CompareTo(b.Center.y));

		for (int i = 0; i + 1 < rooms.Count; i++)
			Corridor(walk, spec, rooms[i].Center, rooms[i + 1].Center, rng);

		int loops = Mathf.Max(2, Mathf.FloorToInt(rooms.Count * 0.35f));
		List<(Rect, Rect)> pairs = new List<(Rect, Rect)>();

		for (int i = 0; i < rooms.Count; i++)
		{
			for (int j = i + 1; j < rooms.Count; j++)
			{
				if (Vector2Int.Distance(rooms[i].Center, rooms[j].Center) <= 12.0f)
					pairs.Add((rooms[i], rooms[j]));
			}
		}

		Shuffle(pairs, rng);
		for (int i = 0; i < Mathf.Min(loops, pairs.Count); i++)
			Corridor(walk, spec, pairs[i].Item1.Center, pairs[i].Item2.Center, rng);

		List<Vector2Int> walkable = new List<Vector2Int>();
		for (int y = 0; y < spec.Height; y++)
		{
			for (int x = 0; x < spec.Width; x++)
			{
				if (walk[y, x])
					walkable.Add(new Vector2Int(x, y));
			}
		}

		if (InWalkBudget(spec, walkable.Count) == false)
			return null;

		return Finish(spec, walk, walkable, rooms, rng);
	}

	static void Split(Rect rect, System.Random rng, List<Rect> into)
	{
		bool canH = rect.H >= LeafMin * 2;
		bool canV = rect.W >= LeafMin * 2;

		if (canH == false && canV == false)
		{
			into.Add(rect);
			return;
		}

		bool vertical;
		if (rect.W > rect.H * 1.6f && canV)
			vertical = true;
		else if (rect.H > rect.W * 1.6f && canH)
			vertical = false;
		else if (canH && canV)
			vertical = rect.W >= rect.H;
		else
			vertical = canV;

		if (rect.W <= 12 && rect.H <= 12 && rng.NextDouble() < 0.35)
		{
			into.Add(rect);
			return;
		}

		double t = 0.40 + rng.NextDouble() * 0.20;

		if (vertical)
		{
			int cut = (int)(rect.W * t);
			if (cut < LeafMin || rect.W - cut < LeafMin)
			{
				into.Add(rect);
				return;
			}

			Split(new Rect(rect.X, rect.Y, cut, rect.H), rng, into);
			Split(new Rect(rect.X + cut, rect.Y, rect.W - cut, rect.H), rng, into);
			return;
		}

		int cutY = (int)(rect.H * t);
		if (cutY < LeafMin || rect.H - cutY < LeafMin)
		{
			into.Add(rect);
			return;
		}

		Split(new Rect(rect.X, rect.Y, rect.W, cutY), rng, into);
		Split(new Rect(rect.X, rect.Y + cutY, rect.W, rect.H - cutY), rng, into);
	}

	static bool TryCarve(Rect leaf, System.Random rng, out Rect room)
	{
		room = default(Rect);

		int maxW = Mathf.Min(leaf.W - 2, RoomMax);
		int maxH = Mathf.Min(leaf.H - 2, RoomMax);

		if (maxW < RoomMin || maxH < RoomMin)
			return false;

		int w = Mathf.Max(rng.Next(RoomMin, maxW + 1), rng.Next(RoomMin, maxW + 1));
		int h = Mathf.Max(rng.Next(RoomMin, maxH + 1), rng.Next(RoomMin, maxH + 1));
		int x = rng.Next(leaf.X + 1, leaf.X + leaf.W - w);
		int y = rng.Next(leaf.Y + 1, leaf.Y + leaf.H - h);

		room = new Rect(x, y, w, h);
		return true;
	}

	static void Corridor(bool[,] walk, Spec spec, Vector2Int a, Vector2Int b, System.Random rng)
	{
		bool horizontalFirst = rng.NextDouble() < 0.5;

		if (horizontalFirst)
		{
			HLine(walk, spec, a.x, b.x, a.y);
			VLine(walk, spec, a.y, b.y, b.x);
			return;
		}

		VLine(walk, spec, a.y, b.y, a.x);
		HLine(walk, spec, a.x, b.x, b.y);
	}

	static void HLine(bool[,] walk, Spec spec, int x0, int x1, int y)
	{
		for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
		{
			for (int dy = 0; dy < CorridorWidth; dy++)
			{
				int yy = y + dy;
				if (x > 0 && x < spec.Width - 1 && yy > 0 && yy < spec.Height - 1)
					walk[yy, x] = true;
			}
		}
	}

	static void VLine(bool[,] walk, Spec spec, int y0, int y1, int x)
	{
		for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++)
		{
			for (int dx = 0; dx < CorridorWidth; dx++)
			{
				int xx = x + dx;
				if (xx > 0 && xx < spec.Width - 1 && y > 0 && y < spec.Height - 1)
					walk[y, xx] = true;
			}
		}
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

	static int[] Bfs(bool[,] walk, Spec spec, Vector2Int start)
	{
		int[] dist = new int[spec.Width * spec.Height];
		for (int i = 0; i < dist.Length; i++)
			dist[i] = -1;

		if (walk[start.y, start.x] == false)
			return dist;

		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		dist[start.y * spec.Width + start.x] = 0;
		queue.Enqueue(start);

		int[] dx = { 1, -1, 0, 0 };
		int[] dy = { 0, 0, 1, -1 };

		while (queue.Count > 0)
		{
			Vector2Int cur = queue.Dequeue();

			for (int k = 0; k < 4; k++)
			{
				int nx = cur.x + dx[k];
				int ny = cur.y + dy[k];

				if (nx < 0 || nx >= spec.Width || ny < 0 || ny >= spec.Height)
					continue;

				if (walk[ny, nx] == false || dist[ny * spec.Width + nx] >= 0)
					continue;

				dist[ny * spec.Width + nx] = dist[cur.y * spec.Width + cur.x] + 1;
				queue.Enqueue(new Vector2Int(nx, ny));
			}
		}

		return dist;
	}

	const int AnchorTries = 48;
	public const int ExitClearance = 3;

	static MapData Finish(Spec spec, bool[,] walk, List<Vector2Int> walkable, List<Rect> rooms,
		System.Random rng)
	{
		float diagonal = Mathf.Sqrt(spec.Width * spec.Width + spec.Height * spec.Height);

		for (int attempt = 0; attempt < AnchorTries; attempt++)
		{
			Vector2Int start = walkable[rng.Next(walkable.Count)];
			int[] field = Bfs(walk, spec, start);

			List<Vector2Int> reach = new List<Vector2Int>();
			foreach (Vector2Int p in walkable)
			{
				if (field[p.y * spec.Width + p.x] >= 0)
					reach.Add(p);
			}

			if (reach.Count < walkable.Count * 0.9f)
				continue;

			List<Vector2Int> candidates = new List<Vector2Int>();
			foreach (Vector2Int p in reach)
			{
				if (InsideAnyRoom(rooms, p) == false)
					continue;

				if (field[p.y * spec.Width + p.x] < diagonal * 0.55f)
					continue;

				candidates.Add(p);
			}

			candidates.Sort(delegate (Vector2Int a, Vector2Int b)
			{
				return field[b.y * spec.Width + b.x] - field[a.y * spec.Width + a.x];
			});

			bool[,] carved = null;
			Vector2Int exit = start;

			foreach (Vector2Int p in candidates)
			{
				if (TryCarveDoorway(walk, spec, p, start, field, out carved) == false)
					continue;

				exit = p;
				break;
			}

			if (carved == null)
				continue;

			walk = carved;
			walkable = Walkables(walk, spec);

			if (InWalkBudget(spec, walkable.Count) == false)
				continue;

			field = Bfs(walk, spec, start);

			reach = new List<Vector2Int>();
			foreach (Vector2Int p in walkable)
			{
				if (field[p.y * spec.Width + p.x] >= 0)
					reach.Add(p);
			}

			List<Vector2Int> artifacts = PickArtifacts(spec, reach, start, exit, field, rng);
			if (artifacts == null)
				continue;

			if (spec.HookNearStart && HasHook(artifacts, field, spec) == false)
				continue;

			List<Vector2Int> spawnPool = new List<Vector2Int>();
			foreach (Vector2Int p in reach)
			{
				if (field[p.y * spec.Width + p.x] >= 7)
					spawnPool.Add(p);
			}

			if (spawnPool.Count < 8)
				continue;

			Shuffle(spawnPool, rng);
			return Compose(spec, walk, start, exit, artifacts, spawnPool, rooms, rng);
		}

		return null;
	}

	static bool HasHook(List<Vector2Int> artifacts, int[] field, Spec spec)
	{
		foreach (Vector2Int a in artifacts)
		{
			int d = field[a.y * spec.Width + a.x];
			if (d >= 5 && d <= 9)
				return true;
		}

		return false;
	}

	static List<Vector2Int> PickArtifacts(Spec spec, List<Vector2Int> reach, Vector2Int start,
		Vector2Int exit, int[] field, System.Random rng)
	{
		List<Vector2Int> pool = new List<Vector2Int>();
		foreach (Vector2Int p in reach)
		{
			if (p == start)
				continue;

			if (Mathf.Max(Mathf.Abs(p.x - exit.x), Mathf.Abs(p.y - exit.y)) < ExitClearance)
				continue;

			pool.Add(p);
		}

		if (pool.Count < spec.Artifacts)
			return null;

		List<Vector2Int> picked = new List<Vector2Int>();

		if (spec.HookNearStart)
		{
			List<Vector2Int> nearStart = new List<Vector2Int>();
			foreach (Vector2Int p in pool)
			{
				int d = field[p.y * spec.Width + p.x];
				if (d >= 5 && d <= 9)
					nearStart.Add(p);
			}

			if (nearStart.Count == 0)
				return null;

			picked.Add(nearStart[rng.Next(nearStart.Count)]);
		}
		else
		{
			picked.Add(pool[rng.Next(pool.Count)]);
		}

		while (picked.Count < spec.Artifacts)
		{
			Vector2Int bestPoint = default(Vector2Int);
			float bestScore = -1.0f;

			foreach (Vector2Int p in pool)
			{
				float nearest = float.MaxValue;
				foreach (Vector2Int q in picked)
					nearest = Mathf.Min(nearest, Vector2Int.Distance(p, q));

				if (nearest > bestScore)
				{
					bestScore = nearest;
					bestPoint = p;
				}
			}

			if (bestScore < 6.0f)
				return null;

			picked.Add(bestPoint);
		}

		int overlaps = 0;
		for (int i = 0; i < picked.Count; i++)
		{
			for (int j = i + 1; j < picked.Count; j++)
			{
				if (Vector2Int.Distance(picked[i], picked[j]) < spec.SoundRadius * 2.0f)
					overlaps++;
			}
		}

		return overlaps > 1 ? null : picked;
	}

	static MapData Compose(Spec spec, bool[,] walk, Vector2Int start, Vector2Int exit,
		List<Vector2Int> artifacts, List<Vector2Int> spawnPool, List<Rect> rooms, System.Random rng)
	{
		TempleCatalog catalog = TempleManifest.Catalog;
		int tileSize = catalog == null ? 64 : catalog.tilePx * catalog.displayScale;

		MapData map = new MapData();
		map.width = spec.Width;
		map.height = spec.Height;
		map.tileSize = tileSize;
		map.pixelWidth = spec.Width * tileSize;
		map.pixelHeight = spec.Height * tileSize;

		int count = spec.Width * spec.Height;
		map.floor = new int[count];
		map.walls = new int[count];
		map.deco = new int[count];

		List<int> plainFloors = TempleManifest.FloorTileIds(false);
		List<int> noisyFloors = TempleManifest.FloorTileIds(true);

		for (int y = 0; y < spec.Height; y++)
		{
			for (int x = 0; x < spec.Width; x++)
			{
				int i = y * spec.Width + x;

				if (walk[y, x])
				{
					map.floor[i] = TempleManifest.TileIdToGid(
						PickFloor(spec, plainFloors, noisyFloors, rng));
					continue;
				}

				bool north = IsWall(walk, spec, x, y - 1);
				bool east = IsWall(walk, spec, x + 1, y);
				bool south = IsWall(walk, spec, x, y + 1);
				bool west = IsWall(walk, spec, x - 1, y);

				map.walls[i] = TempleManifest.TileIdToGid(
					TempleManifest.WallTileId(north, east, south, west));
			}
		}

		map.tileProps = BuildTileProps();
		map.tilesets = new[] { Tileset(1, TempleTilesetName) };

		List<MapPoint> objects = new List<MapPoint>();
		objects.Add(Point("player_start", start, spec.Height));
		objects.Add(Point(MapObjectPlacer.ExitDoorPoint, exit, spec.Height));

		for (int i = 0; i < artifacts.Count; i++)
			objects.Add(Point($"{MapObjectPlacer.ArtifactPrefix}{i + 1}", artifacts[i], spec.Height));

		map.objects = objects.ToArray();

		map.spawns = BuildSpawns(spec, start, exit, spawnPool);
		map.rooms = BuildRooms(rooms);
		return map;
	}

	static bool InsideAnyRoom(List<Rect> rooms, Vector2Int tile)
	{
		foreach (Rect room in rooms)
		{
			if (tile.x >= room.X && tile.x < room.X + room.W
				&& tile.y >= room.Y && tile.y < room.Y + room.H)
				return true;
		}

		return false;
	}

	public static bool InWalkBudget(Spec spec, int walkable)
	{
		float ratio = walkable / (float)(spec.Width * spec.Height);
		return ratio >= MinWalkRatio && ratio <= MaxWalkRatio;
	}

	static List<Vector2Int> Walkables(bool[,] walk, Spec spec)
	{
		List<Vector2Int> list = new List<Vector2Int>();

		for (int row = 0; row < spec.Height; row++)
		{
			for (int col = 0; col < spec.Width; col++)
			{
				if (walk[row, col])
					list.Add(new Vector2Int(col, row));
			}
		}

		return list;
	}

	static bool TryCarveDoorway(bool[,] walk, Spec spec, Vector2Int tile, Vector2Int start,
		int[] before, out bool[,] carved)
	{
		carved = null;

		int half = WallFaceRules.DoorCols / 2;
		int depth = WallFaceRules.DoorRows;

		if (tile.x - half - 1 < 0 || tile.x + half + 1 >= spec.Width || tile.y - depth < 0)
			return false;

		for (int dx = -half; dx <= half; dx++)
		{
			if (walk[tile.y, tile.x + dx] == false)
				return false;
		}

		bool[,] next = (bool[,])walk.Clone();

		for (int d = 1; d <= depth; d++)
			FillRun(next, spec, tile.x, tile.y - d);

		if (next[start.y, start.x] == false)
			return false;

		if (next[tile.y - 1, tile.x - half - 1] || next[tile.y - 1, tile.x + half + 1])
			return false;

		int[] after = Bfs(next, spec, start);

		for (int row = 0; row < spec.Height; row++)
		{
			for (int col = 0; col < spec.Width; col++)
			{
				if (next[row, col] == false)
					continue;

				if (before[row * spec.Width + col] >= 0 && after[row * spec.Width + col] < 0)
					return false;
			}
		}

		carved = next;
		return true;
	}

	static void FillRun(bool[,] walk, Spec spec, int col, int row)
	{
		if (walk[row, col] == false)
			return;

		int left = col;
		while (left - 1 >= 0 && walk[row, left - 1])
			left--;

		int right = col;
		while (right + 1 < spec.Width && walk[row, right + 1])
			right++;

		for (int x = left; x <= right; x++)
			walk[row, x] = false;
	}

	static bool HasExitWallFace(bool[,] walk, Spec spec, Vector2Int tile)
	{
		return WallFaceRules.IsDoorway(delegate (int col, int row)
		{
			if (col < 0 || col >= spec.Width || row < 0 || row >= spec.Height)
				return false;

			return walk[row, col];
		}, tile.x, tile.y, WallFaceRules.DoorCols, WallFaceRules.DoorRows);
	}

	static MapPoint[] BuildSpawns(Spec spec, Vector2Int start, Vector2Int exit,
		List<Vector2Int> spawnPool)
	{
		List<Vector2Int> picked = new List<Vector2Int> { spawnPool[0] };

		while (picked.Count < SpawnCount && picked.Count < spawnPool.Count)
		{
			Vector2Int best = default(Vector2Int);
			float bestScore = -1.0f;

			foreach (Vector2Int candidate in spawnPool)
			{
				if (picked.Contains(candidate))
					continue;

				float nearest = float.MaxValue;
				foreach (Vector2Int chosen in picked)
					nearest = Mathf.Min(nearest, Vector2Int.Distance(candidate, chosen));

				if (nearest > bestScore)
				{
					bestScore = nearest;
					best = candidate;
				}
			}

			if (bestScore < 0.0f)
				break;

			picked.Add(best);
		}

		MapPoint[] spawns = new MapPoint[picked.Count];
		for (int i = 0; i < picked.Count; i++)
			spawns[i] = Point($"SP{i + 1}", picked[i], spec.Height);

		return spawns;
	}

	static MapRoom[] BuildRooms(List<Rect> rooms)
	{
		MapRoom[] built = new MapRoom[rooms.Count];

		for (int i = 0; i < rooms.Count; i++)
		{
			built[i] = new MapRoom
			{
				col = rooms[i].X,
				row = rooms[i].Y,
				width = rooms[i].W,
				height = rooms[i].H,
			};
		}

		return built;
	}

	static bool IsWall(bool[,] walk, Spec spec, int x, int y)
	{
		if (x < 0 || x >= spec.Width || y < 0 || y >= spec.Height)
			return true;

		return walk[y, x] == false;
	}

	static int PickFloor(Spec spec, List<int> plain, List<int> noisy, System.Random rng)
	{
		if (spec.Noisy && noisy.Count > 0 && rng.NextDouble() < NoisyFloorChance)
			return noisy[rng.Next(noisy.Count)];

		if (plain.Count == 0)
			return 0;

		if (plain.Count > 1 && rng.NextDouble() < FloorVariantChance)
			return plain[rng.Next(1, plain.Count)];

		return plain[0];
	}

	static MapTileProp[] BuildTileProps()
	{
		TempleCatalog catalog = TempleManifest.Catalog;
		if (catalog == null || catalog.tiles == null)
			return new MapTileProp[0];

		MapTileProp[] props = new MapTileProp[catalog.tiles.Length];

		for (int i = 0; i < catalog.tiles.Length; i++)
		{
			TempleTile tile = catalog.tiles[i];
			props[i] = Prop(TempleManifest.TileIdToGid(tile.id), tile.walkable, tile.noisy);
		}

		return props;
	}

	static MapTileProp Prop(int gid, bool walkable, bool noisy)
	{
		MapTileProp prop = new MapTileProp();
		prop.gid = gid;
		prop.walkable = walkable;
		prop.noisy = noisy;
		return prop;
	}

	static MapTileset Tileset(int firstGid, string name)
	{
		TempleCatalog catalog = TempleManifest.Catalog;

		MapTileset set = new MapTileset();
		set.firstGid = firstGid;
		set.count = catalog == null || catalog.tiles == null ? 1 : LastTileId(catalog) + 1;
		set.name = name;
		return set;
	}

	static int LastTileId(TempleCatalog catalog)
	{
		int last = 0;
		foreach (TempleTile tile in catalog.tiles)
			last = Mathf.Max(last, tile.id);

		return last;
	}

	static MapPoint Point(string name, Vector2Int tile, int height)
	{
		MapPoint point = new MapPoint();
		point.name = name;
		point.col = tile.x;
		point.row = tile.y;
		point.x = tile.x + 0.5f;
		point.y = height - 1 - tile.y + 0.5f;
		return point;
	}
}
