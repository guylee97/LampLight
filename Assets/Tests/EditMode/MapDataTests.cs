using NUnit.Framework;
using UnityEngine;

public class MapDataTests
{
	[TearDown]
	public void TearDown()
	{
		Managers.Data.UseMap(null);
	}

	[Test]
	public void ValidMapPasses()
	{
		string error;
		Assert.IsTrue(MapTestFixture.Corridor().Validate(out error), error);
	}

	[Test]
	public void LayerLengthMismatchFails()
	{
		MapData map = MapTestFixture.Corridor();
		map.deco = new int[3];

		string error;
		Assert.IsFalse(map.Validate(out error));
		StringAssert.Contains("layer length mismatch", error);
	}

	[Test]
	public void MissingSpawnsFails()
	{
		MapData map = MapTestFixture.Corridor();
		map.spawns = new MapPoint[0];

		string error;
		Assert.IsFalse(map.Validate(out error));
		StringAssert.Contains("spawn", error);
	}

	[Test]
	public void InvalidSizeFails()
	{
		MapData map = MapTestFixture.Corridor();
		map.width = 0;

		string error;
		Assert.IsFalse(map.Validate(out error));
	}

	[Test]
	public void MakeDictIndexesObjectsAndSpawns()
	{
		MapData map = MapTestFixture.Corridor();
		var dict = map.MakeDict();

		Assert.IsTrue(dict.ContainsKey("exit_door"));
		Assert.IsTrue(dict.ContainsKey("SP1"));
	}

	[Test]
	public void TileToWorldRoundTrips()
	{
		MapTestFixture.Install(MapTestFixture.Corridor());

		for (int row = 0; row < MapTestFixture.Height; row++)
		{
			for (int col = 0; col < MapTestFixture.Width; col++)
			{
				Vector3 world = MapCoord.TileToWorld(col, row);
				Vector2Int back = MapCoord.WorldToTile(world);

				Assert.AreEqual(new Vector2Int(col, row), back);
			}
		}
	}

	[Test]
	public void NoisyFloorIsDetectedByTileProps()
	{
		MapTestFixture.Install(MapTestFixture.Corridor());

		Assert.IsTrue(MapCoord.IsNoisy(0, MapTestFixture.NoisyRow));
		Assert.IsFalse(MapCoord.IsNoisy(0, MapTestFixture.NoisyRow + 1));
		Assert.IsFalse(MapCoord.IsNoisy(-1, MapTestFixture.NoisyRow));
	}

	[Test]
	public void NoisyFloorIsDetectedFromWorldPosition()
	{
		MapTestFixture.Install(MapTestFixture.Corridor());

		Assert.IsTrue(MapCoord.IsNoisy(MapCoord.TileToWorld(2, MapTestFixture.NoisyRow)));
		Assert.IsFalse(MapCoord.IsNoisy(MapCoord.TileToWorld(2, MapTestFixture.NoisyRow + 2)));
	}

	[Test]
	public void UnknownGidHasNoTileProp()
	{
		MapData map = MapTestFixture.Corridor();

		Assert.IsNull(map.GetProp(0));
		Assert.IsNull(map.GetProp(99));
		Assert.IsNotNull(map.GetProp(MapTestFixture.NoisyFloorGid));
	}

	[Test]
	public void WallTilesAreNotWalkable()
	{
		MapTestFixture.Install(MapTestFixture.Corridor());

		Assert.IsFalse(MapCoord.IsWalkable(3, 2));
		Assert.IsTrue(MapCoord.IsWalkable(3, 0));
		Assert.IsFalse(MapCoord.IsWalkable(-1, 0));
		Assert.IsFalse(MapCoord.IsWalkable(0, MapTestFixture.Height));
	}
}
