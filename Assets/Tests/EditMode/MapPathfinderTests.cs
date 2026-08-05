using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MapPathfinderTests
{
	readonly List<Vector2Int> _path = new List<Vector2Int>();

	[SetUp]
	public void SetUp()
	{
		MapTestFixture.Install(MapTestFixture.Corridor());
	}

	[TearDown]
	public void TearDown()
	{
		Managers.Data.UseMap(null);
	}

	[Test]
	public void DecorationColliderBlocksTheTileButNotTheWallCheck()
	{
		MapTestFixture.Install(MapTestFixture.DecorationBlock());

		Assert.IsTrue(MapCoord.IsWalkable(MapTestFixture.BlockedCol, MapTestFixture.BlockedRow),
			"장식물이 놓인 칸은 벽 타일이 아니다");
		Assert.IsFalse(MapCoord.IsPassable(MapTestFixture.BlockedCol, MapTestFixture.BlockedRow),
			"장식물 충돌 영역은 통행 불가로 잡혀야 한다");
	}

	[Test]
	public void PathDetoursAroundDecorationCollider()
	{
		MapTestFixture.Install(MapTestFixture.DecorationBlock());

		Assert.IsTrue(MapPathfinder.TryFindPath(new Vector2Int(0, 2), new Vector2Int(6, 2), _path));
		CollectionAssert.DoesNotContain(_path,
			new Vector2Int(MapTestFixture.BlockedCol, MapTestFixture.BlockedRow),
			"경로가 장식물 충돌 영역을 지나면 좀비가 낀다");
	}

	[Test]
	public void FindsPathAroundWall()
	{
		Assert.IsTrue(MapPathfinder.TryFindPath(new Vector2Int(0, 2), new Vector2Int(6, 2), _path));

		Assert.AreEqual(new Vector2Int(0, 2), _path[0]);
		Assert.AreEqual(new Vector2Int(6, 2), _path[_path.Count - 1]);

		foreach (Vector2Int tile in _path)
			Assert.IsTrue(MapCoord.IsWalkable(tile.x, tile.y), $"path crosses a wall at {tile}");
	}

	[Test]
	public void PathStepsAreAdjacent()
	{
		Assert.IsTrue(MapPathfinder.TryFindPath(new Vector2Int(0, 4), new Vector2Int(6, 0), _path));

		for (int i = 1; i < _path.Count; i++)
		{
			int dx = Mathf.Abs(_path[i].x - _path[i - 1].x);
			int dy = Mathf.Abs(_path[i].y - _path[i - 1].y);
			Assert.IsTrue(dx <= 1 && dy <= 1 && dx + dy > 0, $"step {i} jumped from {_path[i - 1]} to {_path[i]}");
		}
	}

	[Test]
	public void RejectsGoalInsideWall()
	{
		Assert.IsFalse(MapPathfinder.TryFindPath(new Vector2Int(0, 2), new Vector2Int(3, 2), _path));
		Assert.AreEqual(0, _path.Count);
	}

	[Test]
	public void DistanceGoesAroundTheWall()
	{
		int straight = 6;
		int distance = MapPathfinder.Distance(0, 2, 6, 2);

		Assert.AreNotEqual(MapPathfinder.Unreachable, distance);
		Assert.Greater(distance, straight);
	}

	[Test]
	public void UnreachableCellReportsUnreachable()
	{
		MapTestFixture.Install(MapTestFixture.Sealed());

		Assert.IsTrue(MapCoord.IsWalkable(5, 3));
		Assert.AreEqual(MapPathfinder.Unreachable, MapPathfinder.Distance(1, 1, 5, 3));
		Assert.IsFalse(MapPathfinder.TryFindPath(new Vector2Int(1, 1), new Vector2Int(5, 3), _path));
	}

	[Test]
	public void DistanceFieldIsCachedByStart()
	{
		int[] first = MapPathfinder.DistanceField(0, 2);
		int[] second = MapPathfinder.DistanceField(0, 2);

		Assert.AreSame(first, second);
	}

	[Test]
	public void InvalidateCacheDropsStaleField()
	{
		int[] first = MapPathfinder.DistanceField(0, 2);
		MapPathfinder.InvalidateCache();
		int[] second = MapPathfinder.DistanceField(0, 2);

		Assert.AreNotSame(first, second);
	}
}
