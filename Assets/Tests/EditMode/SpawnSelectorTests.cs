using NUnit.Framework;

public class SpawnSelectorTests
{
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
	public void PicksTwoDistinctAnchors()
	{
		MapPoint start;
		MapPoint exit;

		Assert.IsTrue(SpawnSelector.TryPickPair(SpawnSelector.Anchors, 1, 64, new System.Random(1), out start, out exit));
		Assert.AreNotSame(start, exit);
	}

	[Test]
	public void RespectsMinimumDistance()
	{
		MapPoint start;
		MapPoint exit;

		Assert.IsTrue(SpawnSelector.TryPickPair(SpawnSelector.Anchors, 8, 256, new System.Random(2), out start, out exit));
		Assert.GreaterOrEqual(MapPathfinder.Distance(start, exit), 8);
	}

	[Test]
	public void FailsWhenMinimumDistanceIsImpossible()
	{
		MapPoint start;
		MapPoint exit;

		Assert.IsFalse(SpawnSelector.TryPickPair(SpawnSelector.Anchors, 999, 64, new System.Random(3), out start, out exit));
		Assert.IsNull(start);
		Assert.IsNull(exit);
	}

	[Test]
	public void FailsWithFewerThanTwoAnchors()
	{
		MapPoint start;
		MapPoint exit;

		MapPoint[] single = { MapTestFixture.Point("only", 0, 2) };

		Assert.IsFalse(SpawnSelector.TryPickPair(single, 0, 8, new System.Random(4), out start, out exit));
	}

	[Test]
	public void IsDeterministicForTheSameSeed()
	{
		MapPoint firstStart;
		MapPoint firstExit;
		MapPoint secondStart;
		MapPoint secondExit;

		SpawnSelector.TryPickPair(SpawnSelector.Anchors, 1, 64, new System.Random(7), out firstStart, out firstExit);
		SpawnSelector.TryPickPair(SpawnSelector.Anchors, 1, 64, new System.Random(7), out secondStart, out secondExit);

		Assert.AreSame(firstStart, secondStart);
		Assert.AreSame(firstExit, secondExit);
	}
}
