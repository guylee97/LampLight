using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LampTests
{
	GameObject _host;
	Lamp _lamp;

	[SetUp]
	public void SetUp()
	{
		Managers.Game.Clear();
		Managers.Game.BeginStage();

		_host = new GameObject("LampHost");
		_lamp = _host.AddComponent<Lamp>();
	}

	[TearDown]
	public void TearDown()
	{
		if (_host != null)
			Object.DestroyImmediate(_host);

		Managers.Game.Clear();
	}

	[Test]
	public void StartsLitWithAFullTank()
	{
		Assert.IsTrue(_lamp.IsOn);
		Assert.IsTrue(_lamp.HasFuel);
		Assert.AreEqual(_lamp.MaxDuration, _lamp.RemainingDuration);
		Assert.AreEqual(1.0f, _lamp.RemainingRatio);
	}

	[Test]
	public void ToggleSwitchesTheLightOffAndOn()
	{
		int toggles = 0;
		bool lastState = true;

		_lamp.OnToggled += state =>
		{
			toggles++;
			lastState = state;
		};

		_lamp.Toggle();

		Assert.IsFalse(_lamp.IsSwitchedOn);
		Assert.IsFalse(_lamp.IsOn);
		Assert.IsTrue(_lamp.HasFuel);
		Assert.AreEqual(1, toggles);
		Assert.IsFalse(lastState);

		_lamp.Toggle();

		Assert.IsTrue(_lamp.IsSwitchedOn);
		Assert.IsTrue(_lamp.IsOn);
		Assert.AreEqual(2, toggles);
		Assert.IsTrue(lastState);
	}

	[Test]
	public void RefillIsRejectedWhenTheTankIsFull()
	{
		Assert.IsFalse(_lamp.Refill(10.0f));
		Assert.AreEqual(_lamp.MaxDuration, _lamp.RemainingDuration);
	}

	[Test]
	public void RefillIsRejectedForNonPositiveAmounts()
	{
		Assert.IsFalse(_lamp.Refill(0));
		Assert.IsFalse(_lamp.Refill(-5.0f));
	}

	[Test]
	public void LightConeAcceptsTargetsInFront()
	{
		_host.transform.position = Vector3.zero;
		_host.transform.rotation = Quaternion.identity;

		Assert.IsTrue(_lamp.IsInLightCone(new Vector3(0, 3, 0)));
	}

	[Test]
	public void LightConeRejectsTargetsBehindAndOutOfRange()
	{
		_host.transform.position = Vector3.zero;
		_host.transform.rotation = Quaternion.identity;

		Assert.IsFalse(_lamp.IsInLightCone(new Vector3(0, -3, 0)));
		Assert.IsFalse(_lamp.IsInLightCone(new Vector3(0, _lamp.Range + 5, 0)));
	}

	[Test]
	public void LightConeRejectsEverythingWhileSwitchedOff()
	{
		_host.transform.position = Vector3.zero;
		_lamp.Toggle();

		Assert.IsFalse(_lamp.IsInLightCone(new Vector3(0, 1, 0)));
	}

	[UnityTest]
	public IEnumerator FuelDrainsWhileLitAndHoldsWhileOff()
	{
		float lit = _lamp.RemainingDuration;

		for (int i = 0; i < 10; i++)
			yield return null;

		float afterBurning = _lamp.RemainingDuration;
		Assert.Less(afterBurning, lit, "켜 둔 램프의 연료가 줄지 않았다");

		_lamp.Toggle();

		for (int i = 0; i < 10; i++)
			yield return null;

		Assert.AreEqual(afterBurning, _lamp.RemainingDuration, 0.0001f,
			"꺼 둔 램프의 연료가 줄었다");
	}

	[UnityTest]
	public IEnumerator OilCanisterRefillsABurnedLampAndIsSpentOnce()
	{
		for (int i = 0; i < 10; i++)
			yield return null;

		float burned = _lamp.RemainingDuration;
		Assert.Less(burned, _lamp.MaxDuration);

		GameObject playerHost = new GameObject("Player");
		_host.transform.SetParent(playerHost.transform);
		PlayerController player = playerHost.AddComponent<PlayerController>();

		GameObject canisterHost = new GameObject("OilCanister");
		canisterHost.AddComponent<BoxCollider2D>();
		OilCanister canister = canisterHost.AddComponent<OilCanister>();

		int used = 0;
		canister.OnUsed += _ => used++;

		canister.Interact(player);
		canister.Interact(player);

		Assert.IsTrue(canister.IsUsed);
		Assert.IsFalse(canister.CanInteract);
		Assert.AreEqual(1, used);
		Assert.Greater(_lamp.RemainingDuration, burned, "기름을 부었는데 연료가 늘지 않았다");

		Object.DestroyImmediate(canisterHost);
		Object.DestroyImmediate(playerHost);
		_host = null;
	}
}
