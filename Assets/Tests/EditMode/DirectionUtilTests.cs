using NUnit.Framework;
using UnityEngine;

public class DirectionUtilTests
{
	[Test]
	public void EveryDirectionRoundTrips()
	{
		for (int i = 0; i < DirectionUtil.Count; i++)
		{
			Define.Direction8 direction = (Define.Direction8)i;
			Vector2 vector = DirectionUtil.ToVector(direction);

			Assert.AreEqual(direction, DirectionUtil.FromVector(vector, Define.Direction8.S));
		}
	}

	[Test]
	public void CardinalVectorsMapToCardinalDirections()
	{
		Assert.AreEqual(Define.Direction8.E, DirectionUtil.FromVector(Vector2.right, Define.Direction8.S));
		Assert.AreEqual(Define.Direction8.N, DirectionUtil.FromVector(Vector2.up, Define.Direction8.S));
		Assert.AreEqual(Define.Direction8.W, DirectionUtil.FromVector(Vector2.left, Define.Direction8.S));
		Assert.AreEqual(Define.Direction8.S, DirectionUtil.FromVector(Vector2.down, Define.Direction8.E));
	}

	[Test]
	public void ZeroVectorKeepsTheFallback()
	{
		Assert.AreEqual(Define.Direction8.NW, DirectionUtil.FromVector(Vector2.zero, Define.Direction8.NW));
	}

	[Test]
	public void DiagonalsMapToDiagonalDirections()
	{
		Assert.AreEqual(Define.Direction8.NE, DirectionUtil.FromVector(new Vector2(1, 1), Define.Direction8.S));
		Assert.AreEqual(Define.Direction8.SW, DirectionUtil.FromVector(new Vector2(-1, -1), Define.Direction8.S));
	}
}
