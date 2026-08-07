using NUnit.Framework;
using UnityEngine;

public class LevelTests
{
	[Test]
	public void FirstLevelNeedsAtLeastOneArtifactBeforeTheRitual()
	{
		GameObject host = new GameObject("Progress");

		try
		{
			StageProgress progress = host.AddComponent<StageProgress>();
			progress.SetRequired(LevelTable.Get(1).ArtifactsRequired);
			progress.ResetProgress();

			Assert.IsFalse(progress.IsComplete, "의식을 치르려면 유물이 최소 하나는 필요하다");

			progress.ReportCollected();
			Assert.IsTrue(progress.IsComplete, "L1은 유물 하나면 의식을 시작할 수 있어야 한다");
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

			Assert.AreEqual(2, progress.Collected, "필요 수가 0이어도 점수용 집계는 계속되어야 한다");
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
	public void ArtifactsPlacedAlwaysExceedRequired()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			LevelConfig config = LevelTable.Get(level);
			Assert.Greater(config.ArtifactsPlaced, config.ArtifactsRequired,
				$"L{level}: 배치 수가 필요 수보다 많아야 선택 여지가 생긴다");
		}
	}

	[Test]
	public void FirstLevelAsksForOneArtifact()
	{
		Assert.AreEqual(1, LevelTable.Get(1).ArtifactsRequired);
	}

	[Test]
	public void RunnerOnlyAppearsOnLastLevel()
	{
		Assert.AreEqual(0, LevelTable.Get(1).RunnerCount);
		Assert.AreEqual(0, LevelTable.Get(2).RunnerCount);
		Assert.AreEqual(1, LevelTable.Get(3).RunnerCount);
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
	public void EnemyCountGrows()
	{
		Assert.Less(LevelTable.Get(1).EnemyCount, LevelTable.Get(2).EnemyCount);
		Assert.Less(LevelTable.Get(2).EnemyCount, LevelTable.Get(3).EnemyCount);
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
	public void SilentRunOutscoresFastRun()
	{
		int silent = ScoreRules.Total(1, 2.0f, 20.0f, false, 0);
		int noisy = ScoreRules.Total(1, 2.0f, 30.0f, true, 0);

		Assert.Greater(silent, noisy, "조용히 간 사람이 이겨야 한다");
	}

	[Test]
	public void ScoreMatchesFormula()
	{
		int score = ScoreRules.Total(2, 2.0f, 30.0f, true, 1);
		Assert.AreEqual(500 * 2 + 300 * 2 + 10 * 30 + 250, score);
	}

	[Test]
	public void ConcealedArtifactsScoreMore()
	{
		int plain = ScoreRules.Total(3, 3.0f, 0.0f, true, 0);
		int hidden = ScoreRules.Total(3, ConcealmentRules.ScoreWeight(1) * 3.0f, 0.0f, true, 0);

		Assert.Greater(hidden, plain, "은닉도가 높을수록 점수가 커야 한다");
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
	public void GradeCutsAreOrdered()
	{
		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			LevelConfig config = LevelTable.Get(level);
			Assert.Greater(config.GradeS, config.GradeA);
			Assert.Greater(config.GradeA, config.GradeB);
		}
	}

	[Test]
	public void GradeResolvesFromScore()
	{
		Assert.AreEqual("S", LevelTable.Grade(1, 1800));
		Assert.AreEqual("A", LevelTable.Grade(1, 1300));
		Assert.AreEqual("B", LevelTable.Grade(1, 900));
		Assert.AreEqual("C", LevelTable.Grade(1, 899));
	}

	[Test]
	public void LampSecondsCoverRecalculatedRoute()
	{
		Assert.AreEqual(60.0f, LevelTable.Get(1).LampSeconds);
		Assert.AreEqual(70.0f, LevelTable.Get(2).LampSeconds);
		Assert.AreEqual(90.0f, LevelTable.Get(3).LampSeconds);
	}
}
