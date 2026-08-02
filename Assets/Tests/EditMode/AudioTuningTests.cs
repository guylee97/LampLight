using NUnit.Framework;
using UnityEngine;

public class AudioTuningTests
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
	public void PingPeriodScalesWithDistance()
	{
		Assert.AreEqual(3.6f, AudioTuning.PingPeriod(12.0f), 0.0001f);
		Assert.AreEqual(1.2f, AudioTuning.PingPeriod(4.0f), 0.0001f);
	}

	[Test]
	public void PingPeriodClampsToFloor()
	{
		Assert.AreEqual(AudioTuning.PingMinPeriod, AudioTuning.PingPeriod(0.5f), 0.0001f);
		Assert.AreEqual(AudioTuning.PingMinPeriod, AudioTuning.PingPeriod(0.0f), 0.0001f);
	}

	[Test]
	public void ClearWeightFallsOffPerWall()
	{
		Assert.AreEqual(1.0f, AudioTuning.ClearWeight(0), 0.0001f);
		Assert.AreEqual(0.45f, AudioTuning.ClearWeight(1), 0.0001f);
		Assert.AreEqual(0.2025f, AudioTuning.ClearWeight(2), 0.0001f);
	}

	[Test]
	public void ClearWeightStaysInRange()
	{
		for (int walls = 0; walls < 12; walls++)
		{
			float weight = AudioTuning.ClearWeight(walls);
			Assert.GreaterOrEqual(weight, 0.0f);
			Assert.LessOrEqual(weight, 1.0f);
		}
	}

	[Test]
	public void BusGainsMatchMixPlan()
	{
		Assert.AreEqual(1.0f, AudioTuning.BusGain(Define.Sound.Threat), 0.0001f);
		Assert.AreEqual(0.5012f, AudioTuning.BusGain(Define.Sound.Guide), 0.001f);
		Assert.AreEqual(0.7079f, AudioTuning.BusGain(Define.Sound.Self), 0.001f);
		Assert.AreEqual(0.0316f, AudioTuning.BusGain(Define.Sound.Ambient), 0.001f);
	}

	[Test]
	public void ArtifactRadiusTightensPerLevel()
	{
		Assert.AreEqual(12.0f, AudioTuning.ArtifactRadius(1));
		Assert.AreEqual(9.0f, AudioTuning.ArtifactRadius(2));
		Assert.AreEqual(7.0f, AudioTuning.ArtifactRadius(3));
	}

	[Test]
	public void RolloffStaysAudibleAcrossRadius()
	{
		AnimationCurve curve = AudioTuning.BuildRolloffCurve();

		Assert.AreEqual(1.0f, curve.Evaluate(0.0f), 0.001f);
		Assert.AreEqual(0.0f, curve.Evaluate(1.0f), 0.001f);

		float atEightTiles = curve.Evaluate(8.0f / 12.0f);
		float db = 20.0f * Mathf.Log10(atEightTiles);
		Assert.Greater(db, -20.0f, "8 tiles must stay well above the old -37.6 dBFS");
	}

	[Test]
	public void CountWallsIgnoresOpenRow()
	{
		Assert.AreEqual(0, MapRaycast.CountWalls(new Vector3(0.5f, 4.5f), new Vector3(6.5f, 4.5f)));
	}

	[Test]
	public void CountWallsFindsCorridorWall()
	{
		Assert.AreEqual(1, MapRaycast.CountWalls(new Vector3(0.5f, 2.5f), new Vector3(6.5f, 2.5f)));
	}

	[Test]
	public void CountWallsIsZeroForSameTile()
	{
		Assert.AreEqual(0, MapRaycast.CountWalls(new Vector3(0.5f, 2.5f), new Vector3(0.5f, 2.5f)));
	}

	[Test]
	public void CountWallsIsSymmetric()
	{
		Vector3 a = new Vector3(0.5f, 2.5f);
		Vector3 b = new Vector3(6.5f, 2.5f);
		Assert.AreEqual(MapRaycast.CountWalls(a, b), MapRaycast.CountWalls(b, a));
	}

	[Test]
	public void HeartbeatSpeedsUpAsFuelDrains()
	{
		float slow = AmbienceController.BeatInterval(20.0f, 20.0f, 1.10f, 0.42f);
		float mid = AmbienceController.BeatInterval(10.0f, 20.0f, 1.10f, 0.42f);
		float fast = AmbienceController.BeatInterval(0.0f, 20.0f, 1.10f, 0.42f);

		Assert.Greater(slow, mid);
		Assert.Greater(mid, fast);
		Assert.AreEqual(0.42f, fast, 0.001f);
	}

	[Test]
	public void FootstepJitterStaysWithinBand()
	{
		Assert.AreEqual(0.55f, EnemyFootstep.NextInterval(0.55f, 0.15f, 0.0f), 0.001f);
		Assert.AreEqual(0.55f * 1.15f, EnemyFootstep.NextInterval(0.55f, 0.15f, 1.0f), 0.001f);
		Assert.AreEqual(0.55f * 0.85f, EnemyFootstep.NextInterval(0.55f, 0.15f, -1.0f), 0.001f);
	}

	[Test]
	public void WalkerRhythmIsRegular()
	{
		Assert.AreEqual(0.9f, EnemyFootstep.NextInterval(0.9f, 0.0f, 1.0f), 0.001f);
		Assert.AreEqual(0.9f, EnemyFootstep.NextInterval(0.9f, 0.0f, -1.0f), 0.001f);
	}

	[Test]
	public void BreathRisesOnlyBelowThreshold()
	{
		Assert.AreEqual(0.0f, PlayerBreath.TargetVolume(1.0f, 0.5f), 0.001f);
		Assert.AreEqual(0.0f, PlayerBreath.TargetVolume(0.5f, 0.5f), 0.001f);
		Assert.AreEqual(0.5f, PlayerBreath.TargetVolume(0.25f, 0.5f), 0.001f);
		Assert.AreEqual(1.0f, PlayerBreath.TargetVolume(0.0f, 0.5f), 0.001f);
	}
}
