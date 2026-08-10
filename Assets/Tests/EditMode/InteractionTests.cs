using NUnit.Framework;
using UnityEngine;

public class InteractionTests
{
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

}
