using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MapGeneratorTests
{
	[TearDown]
	public void TearDown()
	{
		Managers.Data.UseMap(null);
	}

	static List<MapPoint> Artifacts(MapData map)
	{
		List<MapPoint> found = new List<MapPoint>();

		foreach (MapPoint point in map.objects)
		{
			if (point.name.StartsWith(MapObjectPlacer.ArtifactPrefix))
				found.Add(point);
		}

		return found;
	}

	static MapData Generate(int level, int seed)
	{
		int used;
		MapData map = MapGenerator.Generate(level, seed, out used);
		Assert.IsNotNull(map, $"L{level} seed {seed}: 30회 안에 생성 실패");
		return map;
	}

	[Test]
	public void SameSeedProducesSameMap()
	{
		int a, b;
		MapData first = MapGenerator.Generate(2, 4242, out a);
		MapData second = MapGenerator.Generate(2, 4242, out b);

		Assert.IsNotNull(first);
		Assert.IsNotNull(second);
		Assert.AreEqual(a, b, "같은 시드는 같은 시도 번호에서 성공해야 한다");
		CollectionAssert.AreEqual(first.walls, second.walls, "같은 시드가 다른 벽을 만들었다");
		CollectionAssert.AreEqual(first.floor, second.floor, "같은 시드가 다른 바닥을 만들었다");
	}

	[Test]
	public void DifferentSeedsProduceDifferentMaps()
	{
		MapData a = Generate(2, 1);
		MapData b = Generate(2, 900);

		Assert.AreNotEqual(string.Join(",", a.walls), string.Join(",", b.walls),
			"시드가 달라도 같은 맵이 나오면 랜덤 생성이 무의미하다");
	}

	[Test]
	public void GeneratedMapsValidate()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = Generate(level, 100 * level);

			string error;
			Assert.IsTrue(map.Validate(out error), $"L{level}: {error}");
		}
	}

	[Test]
	public void GeneratedMapsMatchLevelSize()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapGenerator.SizeRange range = MapGenerator.SizeRange.For(level);
			MapData map = Generate(level, 7000 + level);

			Assert.GreaterOrEqual(map.width, range.MinWidth, $"L{level} 폭 하한");
			Assert.LessOrEqual(map.width, range.MaxWidth, $"L{level} 폭 상한");
			Assert.GreaterOrEqual(map.height, range.MinHeight, $"L{level} 높이 하한");
			Assert.LessOrEqual(map.height, range.MaxHeight, $"L{level} 높이 상한");
		}
	}

	[Test]
	public void EverySeedHoldsTheHardConstraints()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			float radius = LevelTable.Get(level).ArtifactRadiusTiles;

			for (int seed = 1; seed <= 5; seed++)
			{
				MapData map = Generate(level, seed * 137);
				Managers.Data.UseMap(map);

				MapPoint start = Managers.Data.GetPoint("player_start");
				MapPoint exit = Managers.Data.GetPoint(MapObjectPlacer.ExitDoorPoint);
				List<MapPoint> artifacts = Artifacts(map);

				Assert.AreEqual(LevelTable.Get(level).ArtifactsPlaced, artifacts.Count,
					$"L{level} seed {seed}: 유물 수");

				int[] field = MapPathfinder.DistanceField(start.col, start.row);
				Assert.IsNotNull(field);

				Assert.AreNotEqual(MapPathfinder.Unreachable,
					MapPathfinder.Sample(field, exit.col, exit.row),
					$"L{level} seed {seed}: C1 탈출구 도달 불가");

				float diagonal = Mathf.Sqrt(map.width * map.width + map.height * map.height);
				Assert.GreaterOrEqual(MapPathfinder.Distance(start, exit), diagonal * 0.55f,
					$"L{level} seed {seed}: C2 위반");

				int overlaps = 0;
				for (int i = 0; i < artifacts.Count; i++)
				{
					for (int j = i + 1; j < artifacts.Count; j++)
					{
						Vector2Int a = new Vector2Int(artifacts[i].col, artifacts[i].row);
						Vector2Int b = new Vector2Int(artifacts[j].col, artifacts[j].row);

						Assert.GreaterOrEqual(Vector2Int.Distance(a, b), 6.0f,
							$"L{level} seed {seed}: C5 위반");

						if (Vector2Int.Distance(a, b) < radius * 2.0f)
							overlaps++;
					}
				}

				Assert.LessOrEqual(overlaps, 1, $"L{level} seed {seed}: C6 위반");

				foreach (MapPoint artifact in artifacts)
				{
					Assert.AreNotEqual(MapPathfinder.Unreachable,
						MapPathfinder.Sample(field, artifact.col, artifact.row),
						$"L{level} seed {seed}: C1 유물 도달 불가");
				}

				foreach (MapPoint spawn in map.spawns)
				{
					Assert.GreaterOrEqual(MapPathfinder.Sample(field, spawn.col, spawn.row), 7,
						$"L{level} seed {seed}: C3 위반 — 시작 7타일 안에 스폰");
				}
			}
		}
	}

	[Test]
	public void FirstLevelKeepsTheOpeningHook()
	{
		for (int seed = 1; seed <= 5; seed++)
		{
			MapData map = Generate(1, seed * 91);
			Managers.Data.UseMap(map);

			MapPoint start = Managers.Data.GetPoint("player_start");
			int[] field = MapPathfinder.DistanceField(start.col, start.row);

			bool near = false;
			foreach (MapPoint artifact in Artifacts(map))
			{
				int d = MapPathfinder.Sample(field, artifact.col, artifact.row);
				if (d >= 5 && d <= 9)
					near = true;
			}

			Assert.IsTrue(near, $"seed {seed}: C10 위반 — L1 시작 근처에 유물이 없다");
		}
	}

	[Test]
	public void CorridorsAreTwoTilesWide()
	{
		MapData map = Generate(3, 555);
		Managers.Data.UseMap(map);

		int pinch = 0;

		for (int row = 1; row < map.height - 1; row++)
		{
			for (int col = 1; col < map.width - 1; col++)
			{
				if (MapCoord.IsWalkable(col, row) == false)
					continue;

				bool horizontalPinch = MapCoord.IsWalkable(col, row - 1) == false
					&& MapCoord.IsWalkable(col, row + 1) == false;
				bool verticalPinch = MapCoord.IsWalkable(col - 1, row) == false
					&& MapCoord.IsWalkable(col + 1, row) == false;

				if (horizontalPinch || verticalPinch)
					pinch++;
			}
		}

		float ratio = pinch / (float)(map.width * map.height);
		Assert.Less(ratio, 0.05f,
			$"1타일 병목이 {pinch}칸 — 복도 폭 2타일 규칙이 무너지면 좀비 회피가 불가능해진다");
	}
}
