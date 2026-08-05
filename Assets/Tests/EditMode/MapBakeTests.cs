using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MapBakeTests
{
	[TearDown]
	public void TearDown()
	{
		Managers.Data.UseMap(null);
	}

	static MapData Load(int level)
	{
		Assert.IsTrue(Managers.Data.LoadLevelMap(level), $"L{level} 맵을 로드하지 못했다");
		return Managers.Data.Map;
	}

	static Vector2Int Tile(MapPoint point)
	{
		return new Vector2Int(point.col, point.row);
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

	[Test]
	public void EveryLevelMapMatchesItsSpec()
	{
		int[,] expected = { { 1, 30, 28 }, { 2, 41, 40 }, { 3, 56, 52 } };

		for (int i = 0; i < 3; i++)
		{
			MapData map = Load(expected[i, 0]);
			Assert.AreEqual(expected[i, 1], map.width, $"L{expected[i, 0]} 폭");
			Assert.AreEqual(expected[i, 2], map.height, $"L{expected[i, 0]} 높이");
			Assert.AreEqual(64, map.tileSize);
		}
	}

	[Test]
	public void ArtifactCountMatchesLevelConfig()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = Load(level);
			Assert.AreEqual(LevelTable.Get(level).ArtifactsPlaced, Artifacts(map).Count,
				$"L{level} 유물 배치 수");
		}
	}

	[Test]
	public void C1_StartReachesEveryArtifactAndExit()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = Load(level);
			MapPoint start = Managers.Data.GetPoint("player_start");
			Assert.IsNotNull(start, $"L{level} player_start 없음");

			int[] field = MapPathfinder.DistanceField(start.col, start.row);
			Assert.IsNotNull(field, $"L{level} 거리 필드 실패");

			MapPoint exit = Managers.Data.GetPoint(MapObjectPlacer.ExitDoorPoint);
			Assert.AreNotEqual(MapPathfinder.Unreachable,
				MapPathfinder.Sample(field, exit.col, exit.row), $"L{level} 탈출구 도달 불가");

			foreach (MapPoint artifact in Artifacts(map))
			{
				Assert.AreNotEqual(MapPathfinder.Unreachable,
					MapPathfinder.Sample(field, artifact.col, artifact.row),
					$"L{level} {artifact.name} 도달 불가");
			}
		}
	}

	[Test]
	public void C2_ExitIsFarFromStart()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = Load(level);
			MapPoint start = Managers.Data.GetPoint("player_start");
			MapPoint exit = Managers.Data.GetPoint(MapObjectPlacer.ExitDoorPoint);

			float diagonal = Mathf.Sqrt(map.width * map.width + map.height * map.height);
			int distance = MapPathfinder.Distance(start, exit);

			Assert.GreaterOrEqual(distance, diagonal * 0.55f,
				$"L{level} 시작-탈출구가 너무 가깝다 ({distance} < {diagonal * 0.55f:0.0})");
		}
	}

	[Test]
	public void C5_ArtifactsAreSpreadApart()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = Load(level);
			List<MapPoint> artifacts = Artifacts(map);

			for (int i = 0; i < artifacts.Count; i++)
			{
				for (int j = i + 1; j < artifacts.Count; j++)
				{
					float d = Vector2Int.Distance(Tile(artifacts[i]), Tile(artifacts[j]));
					Assert.GreaterOrEqual(d, 6.0f,
						$"L{level} {artifacts[i].name}-{artifacts[j].name}이 6타일 안에 몰렸다");
				}
			}
		}
	}

	[Test]
	public void C6_AtMostOneOverlappingSoundPair()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = Load(level);
			List<MapPoint> artifacts = Artifacts(map);
			float radius = LevelTable.Get(level).ArtifactRadiusTiles;

			int overlaps = 0;
			for (int i = 0; i < artifacts.Count; i++)
			{
				for (int j = i + 1; j < artifacts.Count; j++)
				{
					if (Vector2Int.Distance(Tile(artifacts[i]), Tile(artifacts[j])) < radius * 2.0f)
						overlaps++;
				}
			}

			Assert.LessOrEqual(overlaps, 1,
				$"L{level} 소리 반경이 겹치는 유물 쌍이 {overlaps}개 — 거리 판별이 불가능해진다");
		}
	}

	[Test]
	public void C9_ExitDoorSitsOnReachableFloor()
	{
		System.Collections.Generic.List<string> bad = new System.Collections.Generic.List<string>();

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = Load(level);
			MapPoint exit = map.Find(MapObjectPlacer.ExitDoorPoint);
			MapPoint start = map.Find("player_start");

			if (MapCoord.IsWalkable(exit.col, exit.row) == false)
			{
				bad.Add($"L{level} ({exit.col},{exit.row}) 에 설 수 없다");
				continue;
			}

			if (MapPathfinder.Distance(start, exit) == MapPathfinder.Unreachable)
				bad.Add($"L{level} 시작점에서 ({exit.col},{exit.row}) 로 갈 수 없다");
		}

		Assert.IsEmpty(bad, "출구 계단: " + string.Join(", ", bad));
	}

	[Test]
	public void C10_FirstLevelHasAnArtifactNearTheStart()
	{
		MapData map = Load(1);
		System.Collections.Generic.List<string> bad = new System.Collections.Generic.List<string>();

		foreach (MapPoint spawn in map.spawns)
		{
			int[] field = MapPathfinder.DistanceField(spawn.col, spawn.row);
			bool found = false;

			foreach (MapPoint artifact in Artifacts(map))
			{
				int d = MapPathfinder.Sample(field, artifact.col, artifact.row);
				if (d >= 5 && d <= 9)
					found = true;
			}

			if (found == false)
				bad.Add($"({spawn.col},{spawn.row})");
		}

		Assert.IsEmpty(bad, "L1은 어느 시작점에서든 5~9타일 안에 유물이 있어야 "
			+ "2초 내 첫 리듬음이 난다. 위반: " + string.Join(", ", bad));
	}

	[Test]
	public void SpawnsAreWalkableAndAwayFromStart()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = Load(level);
			MapPoint start = Managers.Data.GetPoint("player_start");
			int[] field = MapPathfinder.DistanceField(start.col, start.row);

			Assert.GreaterOrEqual(map.spawns.Length, 8, $"L{level} 스폰 앵커 부족");

			foreach (MapPoint spawn in map.spawns)
			{
				Assert.IsTrue(MapCoord.IsWalkable(spawn.col, spawn.row),
					$"L{level} {spawn.name}이 벽 위에 있다");
				Assert.GreaterOrEqual(MapPathfinder.Sample(field, spawn.col, spawn.row), 7,
					$"L{level} {spawn.name}이 시작 7타일 안에 있다");
			}
		}
	}

	[Test]
	public void WalkableRatioStaysInBand()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = Load(level);

			int walkable = 0;
			for (int row = 0; row < map.height; row++)
			{
				for (int col = 0; col < map.width; col++)
				{
					if (MapCoord.IsWalkable(col, row))
						walkable++;
				}
			}

			float ratio = walkable / (float)(map.width * map.height);
			Assert.GreaterOrEqual(ratio, 0.30f, $"L{level} 보행 비율이 너무 낮다 ({ratio:0.00})");
			Assert.LessOrEqual(ratio, 0.52f, $"L{level} 보행 비율이 너무 높다 ({ratio:0.00})");
		}
	}
}
