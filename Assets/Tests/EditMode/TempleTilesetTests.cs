using System.Collections.Generic;
using NUnit.Framework;

public class TempleTilesetTests
{
	[SetUp]
	public void SetUp()
	{
		TempleManifest.Invalidate();
	}

	[Test]
	public void CatalogLoadsWithTilesAndObjects()
	{
		TempleCatalog catalog = TempleManifest.Catalog;

		Assert.IsNotNull(catalog, "temple_catalog.json 이 로드되지 않았다");
		Assert.Greater(catalog.tiles.Length, 0);
		Assert.Greater(catalog.objects.Length, 0);
		Assert.AreEqual(32, catalog.tilePx);
		Assert.AreEqual(8, catalog.autotileBase);
	}

	[Test]
	public void AutotileMaskFollowsManifestBits()
	{
		TempleCatalog catalog = TempleManifest.Catalog;

		Assert.AreEqual(catalog.autotileBase, TempleManifest.WallTileId(false, false, false, false));
		Assert.AreEqual(catalog.autotileBase + catalog.maskNorth,
			TempleManifest.WallTileId(true, false, false, false));
		Assert.AreEqual(catalog.autotileBase + catalog.maskWest,
			TempleManifest.WallTileId(false, false, false, true));
		Assert.AreEqual(catalog.autotileBase + 15,
			TempleManifest.WallTileId(true, true, true, true));
	}

	[Test]
	public void EveryAutotileVariantExistsInCatalog()
	{
		TempleCatalog catalog = TempleManifest.Catalog;

		for (int mask = 0; mask < 16; mask++)
		{
			TempleTile tile = catalog.Tile(catalog.autotileBase + mask);
			Assert.IsNotNull(tile, $"오토타일 {catalog.autotileBase + mask} 없음");
			Assert.IsFalse(tile.walkable, $"오토타일 {tile.id} 이 통행 가능으로 잡혀 있다");
		}
	}

	[Test]
	public void GidRoundTrips()
	{
		Assert.AreEqual(1, TempleManifest.TileIdToGid(0));
		Assert.AreEqual(0, TempleManifest.GidToTileId(1));
	}

	[Test]
	public void GeneratedMapUsesOnlyCatalogTiles()
	{
		int seed;
		MapData map = MapGenerator.Generate(1, 20260801, out seed);

		Assert.IsNotNull(map);

		for (int i = 0; i < map.floor.Length; i++)
		{
			if (map.floor[i] == 0)
				continue;

			TempleTile tile = TempleManifest.Catalog.Tile(TempleManifest.GidToTileId(map.floor[i]));
			Assert.IsNotNull(tile, $"floor gid {map.floor[i]} 이 카탈로그에 없다");
			Assert.IsTrue(tile.walkable, $"바닥에 통행 불가 타일 {tile.name} 이 깔렸다");
		}

		for (int i = 0; i < map.walls.Length; i++)
		{
			if (map.walls[i] == 0)
				continue;

			TempleTile tile = TempleManifest.Catalog.Tile(TempleManifest.GidToTileId(map.walls[i]));
			Assert.IsNotNull(tile, $"wall gid {map.walls[i]} 이 카탈로그에 없다");
			Assert.IsFalse(tile.walkable, $"벽에 통행 가능 타일 {tile.name} 이 깔렸다");
		}
	}

	[Test]
	public void WallMaskMatchesNeighbours()
	{
		int seed;
		MapData map = MapGenerator.Generate(2, 7, out seed);
		Assert.IsNotNull(map);

		TempleCatalog catalog = TempleManifest.Catalog;

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				int gid = map.GetGid(map.walls, col, row);
				if (gid == 0)
					continue;

				int expected = 0;
				if (IsWall(map, col, row - 1)) expected |= catalog.maskNorth;
				if (IsWall(map, col + 1, row)) expected |= catalog.maskEast;
				if (IsWall(map, col, row + 1)) expected |= catalog.maskSouth;
				if (IsWall(map, col - 1, row)) expected |= catalog.maskWest;

				int actual = TempleManifest.GidToTileId(gid) - catalog.autotileBase;
				Assert.AreEqual(expected, actual, $"({col},{row}) 마스크 불일치");
			}
		}
	}

	[Test]
	public void GeneratedMapKeepsRoomRects()
	{
		int seed;
		MapData map = MapGenerator.Generate(3, 42, out seed);

		Assert.IsNotNull(map);
		Assert.IsNotNull(map.rooms);
		Assert.GreaterOrEqual(map.rooms.Length, 8);

		foreach (MapRoom room in map.rooms)
		{
			Assert.GreaterOrEqual(room.width, MapGenerator.RoomMin);
			Assert.GreaterOrEqual(room.height, MapGenerator.RoomMin);
			Assert.IsTrue(map.Contains(room.col, room.row));
			Assert.IsTrue(map.Contains(room.Right, room.Bottom));
		}
	}

	static bool IsWall(MapData map, int col, int row)
	{
		if (map.Contains(col, row) == false)
			return true;

		return map.GetGid(map.walls, col, row) != 0;
	}
}
