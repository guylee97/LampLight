using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DecoSpecTests
{
	const int Seeds = 20;

	MapData[] _maps;

	[SetUp]
	public void SetUp()
	{
		TempleManifest.Invalidate();
		_maps = new MapData[LevelTable.MaxLevel + 1];

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			int used;
			_maps[level] = MapGenerator.Generate(level, 20260801 + level, out used);
			Assert.IsNotNull(_maps[level], $"L{level} 맵 생성 실패");
		}
	}

	[TearDown]
	public void TearDown()
	{
		Managers.Data.UseMap(null);
	}

	List<DecoPlacement> Plan(int level, int seed)
	{
		Managers.Data.UseMap(_maps[level]);
		return MapDecoPlan.Build(_maps[level], seed, level);
	}

	static bool Walkable(MapData map, int col, int row)
	{
		return map.Contains(col, row) && map.GetGid(map.walls, col, row) == 0;
	}

	static int Count(List<DecoPlacement> plan, string key)
	{
		int total = 0;
		foreach (DecoPlacement placement in plan)
		{
			if (placement.Key == key)
				total++;
		}

		return total;
	}

	[Test]
	public void PlanIsDeterministic()
	{
		List<DecoPlacement> a = Plan(2, 7);
		List<DecoPlacement> b = Plan(2, 7);

		Assert.AreEqual(a.Count, b.Count);
		for (int i = 0; i < a.Count; i++)
			Assert.AreEqual(a[i].Key, b[i].Key);
	}

	[Test]
	public void NoBannedAssetIsPlaced()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			for (int seed = 0; seed < Seeds; seed++)
			{
				foreach (DecoPlacement placement in Plan(level, seed))
				{
					Assert.IsFalse(DecoSpec.IsBanned(placement.Key),
						$"L{level} seed {seed}: 금지 에셋 {placement.Key}");
				}
			}
		}
	}

	[Test]
	public void EveryPlacedKeyExistsInTheCatalog()
	{
		foreach (DecoPlacement placement in Plan(3, 3))
			Assert.IsNotNull(TempleManifest.Catalog.Object(placement.Key), placement.Key);
	}

	[Test]
	public void ContainersAreLevelThreeOnly()
	{
		for (int level = 1; level < 3; level++)
		{
			for (int seed = 0; seed < Seeds; seed++)
			{
				List<DecoPlacement> plan = Plan(level, seed);

				Assert.AreEqual(0, Count(plan, DecoSpec.SarcophagusClosed),
					$"L{level} seed {seed}: 석관은 L3 전용");
				Assert.AreEqual(0, Count(plan, DecoSpec.DrawerClosed),
					$"L{level} seed {seed}: 서랍은 L3 전용");
			}
		}
	}

	[Test]
	public void SarcophagusStaysWithinMapBudget()
	{
		for (int seed = 0; seed < Seeds; seed++)
		{
			Assert.LessOrEqual(Count(Plan(3, seed), DecoSpec.SarcophagusClosed),
				DecoSpec.SarcophagusPerMapMax, $"seed {seed}: 석관이 맵당 상한을 넘었다");
		}
	}

	[Test]
	public void PillarsAreLevelTwoAndAboveOnly()
	{
		for (int seed = 0; seed < Seeds; seed++)
		{
			foreach (DecoPlacement placement in Plan(1, seed))
			{
				Assert.IsFalse(placement.Key.StartsWith("prop_pillar"),
					$"L1 seed {seed}: 기둥은 L2 부터인데 {placement.Key} 이 나왔다");
			}
		}
	}

	[Test]
	public void PillarsComeInPairsOnTheSameRow()
	{
		for (int level = 2; level <= LevelTable.MaxLevel; level++)
		{
			for (int seed = 0; seed < Seeds; seed++)
			{
				Dictionary<int, int> byRow = new Dictionary<int, int>();

				foreach (DecoPlacement placement in Plan(level, seed))
				{
					if (placement.Key.StartsWith("prop_pillar") == false)
						continue;

					int row = Mathf.FloorToInt(placement.TileY);
					byRow[row] = byRow.ContainsKey(row) ? byRow[row] + 1 : 1;
				}

				foreach (KeyValuePair<int, int> pair in byRow)
				{
					Assert.AreEqual(0, pair.Value % 2,
						$"L{level} seed {seed}: 행 {pair.Key} 기둥 {pair.Value}개 — 쌍이 아니다");
				}
			}
		}
	}

	[Test]
	public void GlassStaysWithinCountAndSpacing()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapPoint start = _maps[level].Find("player_start");

			for (int seed = 0; seed < Seeds; seed++)
			{
				List<Vector2Int> glass = new List<Vector2Int>();

				foreach (DecoPlacement placement in Plan(level, seed))
				{
					if (placement.Key != "noise_glass")
						continue;

					glass.Add(new Vector2Int(Mathf.FloorToInt(placement.TileX),
						Mathf.FloorToInt(placement.TileY)));
				}

				Assert.LessOrEqual(glass.Count, DecoSpec.GlassCount(level),
					$"L{level} seed {seed}: 유리 {glass.Count}개");

				for (int i = 0; i < glass.Count; i++)
				{
					int fromStart = Mathf.Max(Mathf.Abs(glass[i].x - start.col),
						Mathf.Abs(glass[i].y - start.row));

					Assert.GreaterOrEqual(fromStart, DecoSpec.GlassStartClearance,
						$"L{level} seed {seed}: 유리가 시작점 {fromStart}칸");

					for (int j = i + 1; j < glass.Count; j++)
					{
						int gap = Mathf.Max(Mathf.Abs(glass[i].x - glass[j].x),
							Mathf.Abs(glass[i].y - glass[j].y));

						Assert.GreaterOrEqual(gap, DecoSpec.GlassGapTiles,
							$"L{level} seed {seed}: 유리끼리 {gap}칸");
					}
				}
			}
		}
	}

	[Test]
	public void PlanksAreLevelTwoAndAboveOnly()
	{
		for (int seed = 0; seed < Seeds; seed++)
			Assert.AreEqual(0, Count(Plan(1, seed), "noise_planks"), $"L1 seed {seed}: 판자는 L2 부터");
	}

	[Test]
	public void DebrisStaysNearTheSpecifiedRatio()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = _maps[level];
			int walkable = 0;

			for (int row = 0; row < map.height; row++)
			{
				for (int col = 0; col < map.width; col++)
				{
					if (Walkable(map, col, row))
						walkable++;
				}
			}

			int debris = 0;
			foreach (DecoPlacement placement in Plan(level, 5))
			{
				foreach (string key in DecoSpec.DebrisKeys)
				{
					if (placement.Key == key)
						debris++;
				}
			}

			int allowed = Mathf.RoundToInt(walkable * DecoSpec.DebrisRatio) + 2;
			Assert.LessOrEqual(debris, allowed,
				$"L{level}: 잔해 {debris}개 (통행칸 {walkable}, 허용 {allowed})");
		}
	}

	[Test]
	public void NothingSitsOnAnArtifactOrExitTile()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = _maps[level];

			for (int seed = 0; seed < Seeds; seed++)
			{
				foreach (DecoPlacement placement in Plan(level, seed))
				{
					if (placement.Key.StartsWith(MapDecoPlan.CategoryWallDeco))
						continue;

					int col = Mathf.FloorToInt(placement.TileX);
					int row = Mathf.FloorToInt(placement.TileY);

					foreach (MapPoint point in map.objects)
					{
						Assert.IsFalse(col == point.col && row == point.row,
							$"L{level} seed {seed}: {point.name} 위에 {placement.Key}");
					}
				}
			}
		}
	}

	[Test]
	public void MossStaysNearTheSpecifiedRatio()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = _maps[level];
			int wallAdjacent = 0;

			for (int row = 0; row < map.height; row++)
			{
				for (int col = 0; col < map.width; col++)
				{
					if (Walkable(map, col, row) == false)
						continue;

					bool touches = Walkable(map, col + 1, row) == false
						|| Walkable(map, col - 1, row) == false
						|| Walkable(map, col, row + 1) == false
						|| Walkable(map, col, row - 1) == false;

					if (touches)
						wallAdjacent++;
				}
			}

			int moss = 0;
			foreach (DecoPlacement placement in Plan(level, 5))
			{
				foreach (string key in DecoSpec.MossKeys)
				{
					if (placement.Key == key)
						moss++;
				}
			}

			int allowed = Mathf.RoundToInt(wallAdjacent * DecoSpec.MossRatio) + 2;
			Assert.LessOrEqual(moss, allowed,
				$"L{level}: 이끼 {moss}개 (벽 인접 {wallAdjacent}, 허용 {allowed})");
		}
	}

	[Test]
	public void WallMuralCoversOnlyWallTiles()
	{
		List<string> bad = new List<string>();

		for (int level = 2; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = _maps[level];

			for (int seed = 0; seed < Seeds; seed++)
			{
				foreach (DecoPlacement placement in Plan(level, seed))
				{
					if (placement.Key.StartsWith(MapDecoPlan.CategoryWallDeco) == false)
						continue;

					TempleObject entry = TempleManifest.Catalog.Object(placement.Key);
					int tilesWide = Mathf.RoundToInt(entry.w / 32.0f);
					int tilesTall = Mathf.RoundToInt(entry.h / 32.0f);

					int left = Mathf.FloorToInt(placement.TileX - tilesWide * 0.5f);
					int bottom = Mathf.FloorToInt(placement.TileY);

					for (int r = bottom; r > bottom - tilesTall; r--)
					{
						for (int c = left; c < left + tilesWide; c++)
						{
							if (map.Contains(c, r) == false)
							{
								bad.Add($"L{level} seed {seed}: {placement.Key} 이 맵 밖({c},{r})을 덮는다");
								continue;
							}

							if (Walkable(map, c, r))
								bad.Add($"L{level} seed {seed}: {placement.Key} 이 통행 가능 칸({c},{r})을 덮는다");
						}
					}
				}
			}
		}

		Assert.IsEmpty(bad, $"위반 {bad.Count}건\n"
			+ string.Join("\n", bad.GetRange(0, Mathf.Min(8, bad.Count))));
	}

	[Test]
	public void WallDecoSitsOnANorthWallFace()
	{
		List<string> bad = new List<string>();
		int found = 0;

		for (int level = 2; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = _maps[level];

			for (int seed = 0; seed < Seeds; seed++)
			{
				foreach (DecoPlacement placement in Plan(level, seed))
				{
					if (placement.Key.StartsWith(MapDecoPlan.CategoryWallDeco) == false)
						continue;

					found++;
					int col = Mathf.FloorToInt(placement.TileX);
					int floorRow = Mathf.FloorToInt(placement.TileY) + 1;

					if (WallFaceRules.BlockedBand(map, col, floorRow,
						DecoSpec.WallPatternSpan, DecoSpec.WallPatternSpan) == false)
						bad.Add($"L{level} seed {seed}: {placement.Key} 아래 벽면이 없다 ({col},{floorRow})");
				}
			}
		}

		Assert.Greater(found, 0, "벽 장식이 한 번도 배치되지 않았다");
		Assert.IsEmpty(bad, $"위반 {bad.Count}건\n"
			+ string.Join("\n", bad.GetRange(0, Mathf.Min(6, bad.Count))));
	}

	[Test]
	public void FloorDecoNeverSitsOnAWall()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			MapData map = _maps[level];

			for (int seed = 0; seed < Seeds; seed++)
			{
				foreach (DecoPlacement placement in Plan(level, seed))
				{
					if (placement.Key.StartsWith(MapDecoPlan.CategoryWallDeco))
						continue;

					int col = Mathf.FloorToInt(placement.TileX);
					int row = Mathf.FloorToInt(placement.TileY);

					Assert.IsTrue(Walkable(map, col, row),
						$"L{level} seed {seed}: {placement.Key} 이 벽({col},{row}) 위");
				}
			}
		}
	}
}
