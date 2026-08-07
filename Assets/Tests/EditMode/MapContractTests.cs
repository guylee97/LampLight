using System.Collections.Generic;
using NUnit.Framework;

public class MapContractTests
{
	const int SeedSweep = 60;
	const int FirstSeed = 20260801;

	[SetUp]
	public void SetUp()
	{
		TempleManifest.Invalidate();
		SetPieceCatalog.Invalidate();
	}

	[TearDown]
	public void TearDown()
	{
		Managers.Data.UseMap(null);
	}

	[Test]
	public void GenerationSucceedsForEverySeed()
	{
		List<string> failures = new List<string>();

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			for (int i = 0; i < SeedSweep; i++)
			{
				int seed = FirstSeed + i * 7919;
				int used;

				if (MapGenerator.Generate(level, seed, out used) == null)
					failures.Add($"L{level} seed {seed}");
			}
		}

		Assert.IsEmpty(failures, $"생성 실패 {failures.Count}건: " + string.Join(", ", failures));
	}

	[Test]
	public void EverySeedKeepsArtifactsAndExitReachableFromStart()
	{
		List<string> failures = new List<string>();

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			for (int i = 0; i < SeedSweep; i++)
			{
				int used;
				MapData map = MapGenerator.Generate(level, FirstSeed + i * 104729, out used);
				if (map == null)
					continue;

				Managers.Data.UseMap(map);

				MapPoint start = map.Find("player_start");
				if (start == null)
				{
					failures.Add($"L{level} seed {used}: player_start 없음");
					continue;
				}

				foreach (MapPoint point in map.objects)
				{
					if (point.name == "player_start")
						continue;

					int distance = MapPathfinder.Distance(start, point);
					if (distance == MapPathfinder.Unreachable)
						failures.Add($"L{level} seed {used}: {point.name} 도달 불가");
				}
			}
		}

		Assert.IsEmpty(failures, string.Join("\n", failures));
	}

	[Test]
	public void EverySeedPlacesSpawnsOnWalkableTiles()
	{
		List<string> failures = new List<string>();

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			for (int i = 0; i < SeedSweep; i++)
			{
				int used;
				MapData map = MapGenerator.Generate(level, FirstSeed + i * 15485863, out used);
				if (map == null)
					continue;

				foreach (MapPoint point in map.spawns)
				{
					if (map.GetGid(map.walls, point.col, point.row) != 0)
						failures.Add($"L{level} seed {used}: {point.name} 이 벽 위에 있다");
				}
			}
		}

		Assert.IsEmpty(failures, string.Join("\n", failures));
	}
}
