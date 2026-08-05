using UnityEngine;

public static class MapTestFixture
{
	public const int Width = 7;
	public const int Height = 5;

	public static MapData Corridor()
	{
		int[] walls = new int[Width * Height];
		for (int row = 1; row <= 3; row++)
			walls[row * Width + 3] = 3;

		return Build(walls);
	}

	public static MapData Sealed()
	{
		int[] walls = new int[Width * Height];

		for (int col = 0; col < Width; col++)
		{
			walls[0 * Width + col] = 3;
			walls[(Height - 1) * Width + col] = 3;
		}

		for (int row = 0; row < Height; row++)
		{
			walls[row * Width + 0] = 3;
			walls[row * Width + Width - 1] = 3;
		}

		walls[2 * Width + 5] = 3;
		walls[3 * Width + 4] = 3;

		return Build(walls);
	}

	public const int NoisyFloorGid = 1;
	public const int PlainFloorGid = 2;
	public const int NoisyRow = 0;

	public const int BlockedCol = 3;
	public const int BlockedRow = 2;

	public static MapData DecorationBlock()
	{
		MapData map = Build(new int[Width * Height]);

		map.decorations = new[]
		{
			new MapDecoration
			{
				key = "prop_test_block",
				resource = string.Empty,
				x = BlockedCol,
				y = Height - (Height - 1 - BlockedRow) - 0.5f,
				width = 1.0f,
				height = 1.0f,
				collisionEnabled = true,
				colliderWidth = 0.8f,
				colliderHeight = 0.8f,
			},
		};

		return map;
	}

	public static MapData Build(int[] walls)
	{
		MapData map = new MapData();
		map.width = Width;
		map.height = Height;
		map.tileSize = 64;
		map.floor = BuildFloor();
		map.walls = walls;
		map.deco = new int[Width * Height];
		map.tileProps = new[]
		{
			Prop(NoisyFloorGid, true, true),
			Prop(PlainFloorGid, true, false),
			Prop(3, false, false),
		};
		map.tilesets = new MapTileset[0];
		map.objects = new[] { Point("exit_door", 6, 2) };
		map.spawns = new[] { Point("SP1", 0, 2), Point("SP2", 6, 2), Point("SP3", 1, 2) };
		return map;
	}

	static int[] BuildFloor()
	{
		int[] floor = new int[Width * Height];
		for (int row = 0; row < Height; row++)
		{
			for (int col = 0; col < Width; col++)
				floor[row * Width + col] = row == NoisyRow ? NoisyFloorGid : PlainFloorGid;
		}

		return floor;
	}

	static MapTileProp Prop(int gid, bool walkable, bool noisy)
	{
		MapTileProp prop = new MapTileProp();
		prop.gid = gid;
		prop.walkable = walkable;
		prop.noisy = noisy;
		return prop;
	}

	public static MapPoint Point(string name, int col, int row)
	{
		MapPoint point = new MapPoint();
		point.name = name;
		point.col = col;
		point.row = row;
		point.x = col + 0.5f;
		point.y = Height - 1 - row + 0.5f;
		return point;
	}

	public static void Install(MapData map)
	{
		Managers.Data.UseMap(map);
	}
}
