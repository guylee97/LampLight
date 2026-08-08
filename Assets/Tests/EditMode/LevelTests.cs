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
	public void RitualSecondsGrowPerLevel()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			Assert.Greater(LevelTable.Get(level).RitualSeconds, 0.0f,
				$"L{level}: 의식 채널링 시간이 0이면 클라이맥스가 없다");
		}

		Assert.Less(LevelTable.Get(1).RitualSeconds, LevelTable.Get(3).RitualSeconds,
			"뒤 레벨일수록 제단 앞에 더 오래 묶여 있어야 한다");
	}

	[Test]
	public void FirstLevelFitsInsideOneLampBurn()
	{
		LevelConfig config = LevelTable.Get(1);
		float ritualCost = config.ArtifactsRequired * config.RitualSeconds;

		Assert.LessOrEqual(ritualCost, config.LampSeconds * 0.5f,
			"L1은 의식에 등불의 절반 이상을 쓰면 안 된다 — 찾을 시간이 남지 않는다");
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
