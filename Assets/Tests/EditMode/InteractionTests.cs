using NUnit.Framework;
using UnityEngine;

public class InteractionTests
{
	[Test]
	public void StoneStaysWithinThrowRange()
	{
		Vector3 origin = new Vector3(10.0f, 10.0f, 0.0f);
		Vector3 far = new Vector3(40.0f, 10.0f, 0.0f);

		Vector3 clamped = StoneThrower.ClampTarget(origin, far, 8.0f);

		Assert.AreEqual(8.0f, Vector3.Distance(origin, clamped), 0.001f,
			"사거리를 넘는 목표는 8타일로 잘려야 한다");
	}

	[Test]
	public void StoneKeepsNearTargetAsIs()
	{
		Vector3 origin = Vector3.zero;
		Vector3 near = new Vector3(3.0f, 4.0f, 0.0f);

		Vector3 clamped = StoneThrower.ClampTarget(origin, near, 8.0f);

		Assert.AreEqual(near.x, clamped.x, 0.001f);
		Assert.AreEqual(near.y, clamped.y, 0.001f);
	}

	[Test]
	public void ConcealmentHoldGrowsWithLevel()
	{
		Assert.AreEqual(0.0f, ConcealmentRules.HoldSeconds(0), 0.001f);
		Assert.AreEqual(2.0f, ConcealmentRules.HoldSeconds(1), 0.001f);
		Assert.AreEqual(3.0f, ConcealmentRules.HoldSeconds(2), 0.001f);
	}

	[Test]
	public void ConcealmentClampsOutOfRange()
	{
		Assert.AreEqual(0, ConcealmentRules.Clamp(-3));
		Assert.AreEqual(ConcealmentRules.Max, ConcealmentRules.Clamp(9));
	}

	[Test]
	public void ConcealmentSpreadMatchesLevelPlan()
	{
		Assert.AreEqual(0, ConcealmentRules.ForLevel(1, 0));
		Assert.AreEqual(0, ConcealmentRules.ForLevel(1, 1));

		Assert.LessOrEqual(ConcealmentRules.ForLevel(2, 0), 1);
		Assert.LessOrEqual(ConcealmentRules.ForLevel(2, 1), 1);

		Assert.GreaterOrEqual(ConcealmentRules.ForLevel(3, 0), 1);
		Assert.GreaterOrEqual(ConcealmentRules.ForLevel(3, 1), 1);
	}

	[Test]
	public void OnlyLastLevelGivesStones()
	{
		Assert.AreEqual(0, LevelTable.Get(1).Stones);
		Assert.AreEqual(0, LevelTable.Get(2).Stones);
		Assert.AreEqual(2, LevelTable.Get(3).Stones);
	}

	[Test]
	public void OnlyLastLevelGivesOil()
	{
		Assert.AreEqual(0, LevelTable.Get(1).OilCanisters);
		Assert.AreEqual(0, LevelTable.Get(2).OilCanisters);
		Assert.AreEqual(1, LevelTable.Get(3).OilCanisters);
	}
}
