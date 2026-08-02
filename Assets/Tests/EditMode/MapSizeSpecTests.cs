using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MapSizeSpecTests
{
	const int Seeds = 24;

	[Test]
	public void SizeStaysInsideTheLevelRange([Values(1, 2, 3)] int level)
	{
		MapGenerator.SizeRange range = MapGenerator.SizeRange.For(level);
		List<string> bad = new List<string>();

		for (int seed = 0; seed < Seeds; seed++)
		{
			int used;
			MapData map = MapGenerator.Generate(level, 51000 + seed, out used);
			Assert.IsNotNull(map, $"L{level} seed {51000 + seed} 생성 실패");

			if (map.width < range.MinWidth || map.width > range.MaxWidth
				|| map.height < range.MinHeight || map.height > range.MaxHeight)
				bad.Add($"seed {used} {map.width}x{map.height}");
		}

		Assert.IsEmpty(bad, $"L{level} 규격 {range.MinWidth}~{range.MaxWidth} x "
			+ $"{range.MinHeight}~{range.MaxHeight} 이탈: " + string.Join(", ", bad));
	}

	[Test]
	public void WalkableBudgetMatchesTheSpec([Values(1, 2, 3)] int level)
	{
		List<string> bad = new List<string>();

		for (int seed = 0; seed < Seeds; seed++)
		{
			int used;
			MapData map = MapGenerator.Generate(level, 52000 + seed, out used);

			int walkable = 0;
			for (int row = 0; row < map.height; row++)
			{
				for (int col = 0; col < map.width; col++)
				{
					if (map.GetGid(map.walls, col, row) == 0)
						walkable++;
				}
			}

			float ratio = walkable / (float)(map.width * map.height);

			if (ratio < MapGenerator.MinWalkRatio || ratio > MapGenerator.MaxWalkRatio)
				bad.Add($"seed {used} 비율 {ratio * 100:F1}%");
		}

		Assert.IsEmpty(bad, $"L{level} 보행 비율 35~45% 이탈: " + string.Join(", ", bad));
	}

	[Test]
	public void DifferentSeedsProduceDifferentMaps([Values(1, 2, 3)] int level)
	{
		HashSet<int> distinct = new HashSet<int>();

		for (int seed = 0; seed < Seeds; seed++)
		{
			int used;
			MapGenerator.Generate(level, 53000 + seed, out used);
			distinct.Add(used);
		}

		Assert.GreaterOrEqual(distinct.Count, Seeds,
			$"L{level} 시드 {Seeds}개가 서로 다른 맵 {distinct.Count}개로만 흩어진다");
	}

	[Test]
	public void ReplayingTheUsedSeedRebuildsTheSameMap([Values(1, 2, 3)] int level)
	{
		int used;
		MapData first = MapGenerator.Generate(level, 54000, out used);

		int again;
		MapData second = MapGenerator.Generate(level, used, out again);

		Assert.AreEqual(used, again, "재생 시 시드가 달라진다");
		Assert.AreEqual(first.width, second.width);
		Assert.AreEqual(first.height, second.height);

		for (int row = 0; row < first.height; row++)
		{
			for (int col = 0; col < first.width; col++)
			{
				Assert.AreEqual(first.GetGid(first.walls, col, row),
					second.GetGid(second.walls, col, row), $"({col},{row}) 벽이 다르다");
			}
		}
	}
}
