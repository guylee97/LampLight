using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class HardConstraintTests
{
	const int Seeds = 30;
	const int FirstSeed = 20260801;

	static IEnumerable<(int Level, int Seed, MapData Map)> Maps()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			for (int i = 0; i < Seeds; i++)
			{
				int used;
				MapData map = MapGenerator.Generate(level, FirstSeed + i * 7919, out used);
				if (map != null)
					yield return (level, used, map);
			}
		}
	}

	[SetUp]
	public void SetUp()
	{
		TempleManifest.Invalidate();
	}

	[TearDown]
	public void TearDown()
	{
		Managers.Data.UseMap(null);
	}

	static bool Walkable(MapData map, int col, int row)
	{
		return map.Contains(col, row) && map.GetGid(map.walls, col, row) == 0;
	}

	static bool TouchesWall(MapData map, int col, int row)
	{
		return Walkable(map, col + 1, row) == false || Walkable(map, col - 1, row) == false
			|| Walkable(map, col, row + 1) == false || Walkable(map, col, row - 1) == false;
	}

	static int RoomIndex(MapData map, int col, int row)
	{
		for (int i = 0; i < map.rooms.Length; i++)
		{
			if (map.rooms[i].Contains(col, row))
				return i;
		}

		return -1;
	}

	static int RoomExits(MapData map, MapRoom room)
	{
		int exits = 0;

		for (int col = room.col; col <= room.Right; col++)
		{
			if (Walkable(map, col, room.row - 1)) exits++;
			if (Walkable(map, col, room.Bottom + 1)) exits++;
		}

		for (int row = room.row; row <= room.Bottom; row++)
		{
			if (Walkable(map, room.col - 1, row)) exits++;
			if (Walkable(map, room.Right + 1, row)) exits++;
		}

		return exits;
	}

	[Test]
	public void C1_EverythingIsReachableFromStart()
	{
		List<string> bad = new List<string>();

		foreach ((int level, int seed, MapData map) in Maps())
		{
			Managers.Data.UseMap(map);
			MapPoint start = map.Find("player_start");

			foreach (MapPoint point in map.objects)
			{
				if (point.name == "player_start")
					continue;

				if (MapPathfinder.Distance(start, point) == MapPathfinder.Unreachable)
					bad.Add($"L{level} seed {seed}: {point.name} 도달 불가");
			}
		}

		Assert.IsEmpty(bad, string.Join("\n", bad));
	}

	[Test]
	public void C2_ExitIsFarEnoughFromStart()
	{
		List<string> bad = new List<string>();

		foreach ((int level, int seed, MapData map) in Maps())
		{
			Managers.Data.UseMap(map);

			MapPoint start = map.Find("player_start");
			MapPoint exit = map.Find(MapObjectPlacer.ExitDoorPoint);

			float diagonal = Mathf.Sqrt(map.width * map.width + map.height * map.height);
			int distance = MapPathfinder.Distance(start, exit);

			if (distance < diagonal * 0.55f)
				bad.Add($"L{level} seed {seed}: 시작-출구 {distance} < {diagonal * 0.55f:F1}");
		}

		Assert.IsEmpty(bad, string.Join("\n", bad));
	}

	[Test]
	public void C5_ArtifactsKeepSixTilesApart()
	{
		List<string> bad = new List<string>();

		foreach ((int level, int seed, MapData map) in Maps())
		{
			List<MapPoint> artifacts = new List<MapPoint>();
			foreach (MapPoint point in map.objects)
			{
				if (point.name.StartsWith(MapObjectPlacer.ArtifactPrefix))
					artifacts.Add(point);
			}

			for (int i = 0; i < artifacts.Count; i++)
			{
				for (int j = i + 1; j < artifacts.Count; j++)
				{
					float d = Vector2Int.Distance(
						new Vector2Int(artifacts[i].col, artifacts[i].row),
						new Vector2Int(artifacts[j].col, artifacts[j].row));

					if (d < 6.0f)
						bad.Add($"L{level} seed {seed}: 유물 간 {d:F1}타일");
				}
			}
		}

		Assert.IsEmpty(bad, string.Join("\n", bad));
	}

	[Test]
	public void C8_ExitSitsOnAWallFaceOfAConnectedRoom()
	{
		List<string> bad = new List<string>();

		foreach ((int level, int seed, MapData map) in Maps())
		{
			MapPoint exit = map.Find(MapObjectPlacer.ExitDoorPoint);

			if (TouchesWall(map, exit.col, exit.row) == false)
			{
				bad.Add($"L{level} seed {seed}: 출구({exit.col},{exit.row})가 벽에 안 붙어 있다");
				continue;
			}

			int index = RoomIndex(map, exit.col, exit.row);
			if (index < 0)
			{
				bad.Add($"L{level} seed {seed}: 출구가 방 안이 아니다");
				continue;
			}

			int exits = RoomExits(map, map.rooms[index]);
			if (exits < 2)
				bad.Add($"L{level} seed {seed}: 출구 방의 연결도 {exits} < 2 (막다른 방)");
		}

		Assert.IsEmpty(bad, string.Join("\n", bad));
	}

	[Test]
	public void C10_FirstLevelHasAnArtifactNearTheStart()
	{
		List<string> bad = new List<string>();

		foreach ((int level, int seed, MapData map) in Maps())
		{
			if (level != 1)
				continue;

			Managers.Data.UseMap(map);
			MapPoint start = map.Find("player_start");
			bool near = false;

			foreach (MapPoint point in map.objects)
			{
				if (point.name.StartsWith(MapObjectPlacer.ArtifactPrefix) == false)
					continue;

				int d = MapPathfinder.Distance(start, point);
				if (d >= 5 && d <= 9)
					near = true;
			}

			if (near == false)
				bad.Add($"L1 seed {seed}: 시작 5~9타일 내 유물 없음");
		}

		Assert.IsEmpty(bad, string.Join("\n", bad));
	}

	[Test]
	public void SpawnsKeepSevenTilesFromStart()
	{
		List<string> bad = new List<string>();

		foreach ((int level, int seed, MapData map) in Maps())
		{
			Managers.Data.UseMap(map);
			MapPoint start = map.Find("player_start");

			foreach (MapPoint spawn in map.spawns)
			{
				int d = MapPathfinder.Distance(start, spawn);
				if (d >= 0 && d < 7)
					bad.Add($"L{level} seed {seed}: {spawn.name} 이 시작에서 {d}타일");
			}
		}

		Assert.IsEmpty(bad, string.Join("\n", bad));
	}
}
