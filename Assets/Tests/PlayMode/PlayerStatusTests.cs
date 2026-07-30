using NUnit.Framework;
using UnityEngine;

public class PlayerStatusTests
{
	GameObject _host;
	PlayerStatus _status;

	[SetUp]
	public void SetUp()
	{
		_host = new GameObject("PlayerStatusHost");
		_status = _host.AddComponent<PlayerStatus>();
	}

	[TearDown]
	public void TearDown()
	{
		Object.DestroyImmediate(_host);
	}

	[Test]
	public void StartsAtFullStamina()
	{
		Assert.AreEqual(_status.MaxStamina, _status.Stamina);
		Assert.AreEqual(1.0f, _status.StaminaRatio);
		Assert.IsTrue(_status.CanRun);
	}

	[Test]
	public void RunningDrainsStaminaAndExhaustsThePlayer()
	{
		_status.ConsumeRunStamina(_status.MaxStamina * 2.0f);

		Assert.AreEqual(0.0f, _status.Stamina);
		Assert.IsFalse(_status.CanRun);
	}

	[Test]
	public void StaminaNeverGoesNegative()
	{
		_status.ConsumeRunStamina(1000.0f);

		Assert.AreEqual(0.0f, _status.Stamina);
	}

	[Test]
	public void RecoveringBelowTheThresholdStillBlocksRunning()
	{
		_status.ConsumeRunStamina(_status.MaxStamina * 2.0f);
		_status.RecoverStamina(0.5f);

		Assert.Greater(_status.Stamina, 0.0f);
		Assert.Less(_status.StaminaRatio, 0.25f);
		Assert.IsFalse(_status.CanRun);
	}

	[Test]
	public void RecoveringPastTheThresholdRestoresRunning()
	{
		_status.ConsumeRunStamina(_status.MaxStamina * 2.0f);
		_status.RecoverStamina(2.0f);

		Assert.GreaterOrEqual(_status.StaminaRatio, 0.25f);
		Assert.IsTrue(_status.CanRun);
	}

	[Test]
	public void StaminaNeverExceedsTheMaximum()
	{
		_status.RecoverStamina(1000.0f);

		Assert.AreEqual(_status.MaxStamina, _status.Stamina);
		Assert.AreEqual(1.0f, _status.StaminaRatio);
	}
}
