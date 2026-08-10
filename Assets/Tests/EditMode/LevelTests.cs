using NUnit.Framework;
using UnityEngine;

public class LevelTests
{
	[Test]
	public void FirstLevelNeedsEveryArtifactBeforeTheRitual()
	{
		GameObject host = new GameObject("Progress");

		try
		{
			int required = LevelTable.Get(1).ArtifactsRequired;

			StageProgress progress = host.AddComponent<StageProgress>();
			progress.SetRequired(required);
			progress.ResetProgress();

			for (int i = 0; i < required; i++)
			{
				Assert.IsFalse(progress.IsComplete, $"{i}개만 모은 상태로는 의식을 치를 수 없다");
				progress.ReportCollected();
			}

			Assert.IsTrue(progress.IsComplete, "L1도 놓인 공양물을 전부 모아야 의식이 열린다");
		}
		finally
		{
			Object.DestroyImmediate(host);
		}
	}

	[Test]
	public void OptionalArtifactsStillCount()
	{
		GameObject host = new GameObject("Progress");

		try
		{
			StageProgress progress = host.AddComponent<StageProgress>();
			progress.SetRequired(0);
			progress.ResetProgress();

			progress.ReportCollected();
			progress.ReportCollected();

			Assert.AreEqual(2, progress.Collected, "필요 수가 0이어도 집계는 계속되어야 한다");
		}
		finally
		{
			Object.DestroyImmediate(host);
		}
	}

	[Test]
	public void CompletionFiresOnceAtRequirement()
	{
		GameObject host = new GameObject("Progress");

		try
		{
			StageProgress progress = host.AddComponent<StageProgress>();
			progress.SetRequired(2);
			progress.ResetProgress();

			int fired = 0;
			progress.OnAllArtifactsCollected += () => fired++;

			for (int i = 0; i < 4; i++)
				progress.ReportCollected();

			Assert.AreEqual(1, fired, "필요 수를 넘겨도 완료 이벤트는 한 번만");
			Assert.AreEqual(4, progress.Collected);
		}
		finally
		{
			Object.DestroyImmediate(host);
		}
	}

	[Test]
	public void RadiusTightensPerLevel()
	{
		Assert.AreEqual(12.0f, LevelTable.Get(1).ArtifactRadiusTiles);
		Assert.AreEqual(9.0f, LevelTable.Get(2).ArtifactRadiusTiles);
		Assert.AreEqual(7.0f, LevelTable.Get(3).ArtifactRadiusTiles);
	}

	[Test]
	public void EveryPlacedArtifactIsRequired()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			LevelConfig config = LevelTable.Get(level);
			Assert.AreEqual(config.ArtifactsPlaced, config.ArtifactsRequired,
				$"L{level}: 놓인 공양물은 전부 모아야 의식을 치른다");
		}
	}

	[Test]
	public void FirstLevelAsksForEveryArtifactItPlaces()
	{
		LevelConfig config = LevelTable.Get(1);
		Assert.AreEqual(config.ArtifactsPlaced, config.ArtifactsRequired);
	}

	[Test]
	public void EveryLevelHasAYokai()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			Assert.GreaterOrEqual(LevelTable.Get(level).YokaiCount, 1,
				$"L{level}: 요괴가 없으면 퇴치할 대상이 없다");
		}
	}

	[Test]
	public void RitualSecondsAreTheSameOnEveryLevel()
	{
		const float Unified = 5.0f;

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			Assert.AreEqual(Unified, LevelTable.Get(level).RitualSeconds, 0.0001f,
				$"L{level}: 봉인 시간은 전각마다 같아야 한다 — 플레이 가이드가 시간을 알려주지 않으므로 "
				+ "전각마다 다르면 플레이어가 매번 다시 배워야 한다");
		}
	}

	[Test]
	public void PlacingIsShortAndOnlySealingHoldsThePlayerDown()
	{
		Assert.AreEqual(1.5f, Altar.PlaceSeconds, 0.0001f,
			"공양물을 올리는 것은 잡일이다 — 길면 절정과 구분되지 않는다");

		Assert.Less(Altar.PlaceSeconds, LevelTable.Get(LevelTable.MinLevel).RitualSeconds,
			"올리기가 봉인만큼 길면 마지막 하나가 특별해지지 않는다");
	}

	[Test]
	public void StandingStillNeverEatsTheLamp()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			LevelConfig config = LevelTable.Get(level);

			// 공양물을 하나씩 올리고, 다 올린 다음 따로 봉인한다.
			float standing = config.ArtifactsRequired * Altar.PlaceSeconds + config.RitualSeconds;

			Assert.LessOrEqual(standing, config.LampSeconds * 0.2f,
				$"L{level}: 제단 앞에 서 있는 {standing:0.0}초가 등불의 20%를 넘는다 — "
				+ "어둠 속에 못 박혀 있는 시간이 너무 길다");
		}
	}

	[Test]
	public void NoYokaiOutrunsThePlayer()
	{
		const float PlayerRunSpeed = 4.0f;

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			YokaiSpec spec = YokaiTable.ForLevel(level);

			// 추격 속도가 달리기보다 빠르면 들킨 순간 대응할 방법이 없다.
			// 집요함은 속도가 아니라 포기하지 않는 것으로 만든다.
			Assert.Less(spec.ChaseSpeed, PlayerRunSpeed,
				$"L{level} {spec.Label}: 추격 {spec.ChaseSpeed}가 달리기 {PlayerRunSpeed} 이상이라 "
				+ "발각되면 반드시 잡힌다");
		}
	}

	[Test]
	public void DifficultyGrowsWithoutAddingEnemies()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			Assert.AreEqual(1, LevelTable.Get(level).YokaiCount,
				$"L{level}: 요괴는 전각마다 하나다 — 난이도는 맵과 공양물로 올린다");
		}

		Assert.Less(LevelTable.Get(1).ArtifactsRequired, LevelTable.Get(2).ArtifactsRequired);
		Assert.Less(LevelTable.Get(2).ArtifactsRequired, LevelTable.Get(3).ArtifactsRequired);
	}

	[Test]
	public void LampBurnGrowsWithLevel()
	{
		Assert.Less(LevelTable.Get(1).LampSeconds, LevelTable.Get(2).LampSeconds);
		Assert.Less(LevelTable.Get(2).LampSeconds, LevelTable.Get(3).LampSeconds);
	}

	[Test]
	public void ClampKeepsLevelInRange()
	{
		Assert.AreEqual(LevelTable.MinLevel, LevelTable.Clamp(0));
		Assert.AreEqual(LevelTable.MinLevel, LevelTable.Clamp(-5));
		Assert.AreEqual(LevelTable.MaxLevel, LevelTable.Clamp(99));
		Assert.AreEqual(2, LevelTable.Clamp(2));
	}

	[Test]
	public void ConcealmentTradesSoundForNoise()
	{
		for (int level = 0; level < ConcealmentRules.Max; level++)
		{
			Assert.Greater(ConcealmentRules.RadiusScale(level), ConcealmentRules.RadiusScale(level + 1),
				"은닉도가 오르면 소리 반경은 줄어야 한다");
			Assert.Less(ConcealmentRules.NoiseRadius(level), ConcealmentRules.NoiseRadius(level + 1),
				"은닉도가 오르면 획득 소음은 커야 한다");
		}
	}

	[Test]
	public void LampSecondsCoverRecalculatedRoute()
	{
		for (int level = LevelTable.MinLevel; level < LevelTable.MaxLevel; level++)
		{
			Assert.Less(
				LevelTable.Get(level).LampSeconds,
				LevelTable.Get(level + 1).LampSeconds,
				$"{level + 1}전각은 {level}전각보다 넓으니 등불도 더 길어야 한다");
		}

		Assert.GreaterOrEqual(LevelTable.Get(LevelTable.MinLevel).LampSeconds, 45.0f,
			"1전각 등불이 45초 아래면 초행에 헤맬 여유가 없다");
	}
}
