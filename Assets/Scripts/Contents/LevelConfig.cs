public class LevelConfig
{
	public int Level;
	public int ArtifactsPlaced;
	public int ArtifactsRequired;
	public float ArtifactRadiusTiles;
	public float LampSeconds;
	public int WalkerCount;
	public int WandererCount;
	public int RunnerCount;
	public int OilCanisters;
	public int Stones;
	public int GradeS;
	public int GradeA;
	public int GradeB;

	public int EnemyCount { get { return WalkerCount + WandererCount + RunnerCount; } }
}

public static class LevelTable
{
	public const int MinLevel = 1;
	public const int MaxLevel = 3;

	static readonly LevelConfig[] Levels =
	{
		new LevelConfig
		{
			Level = 1,
			ArtifactsPlaced = 2,
			ArtifactsRequired = 0,
			ArtifactRadiusTiles = 12.0f,
			LampSeconds = 60.0f,
			WalkerCount = 1,
			WandererCount = 0,
			RunnerCount = 0,
			OilCanisters = 0,
			Stones = 0,
			GradeS = 1800,
			GradeA = 1300,
			GradeB = 900,
		},
		new LevelConfig
		{
			Level = 2,
			ArtifactsPlaced = 3,
			ArtifactsRequired = 2,
			ArtifactRadiusTiles = 9.0f,
			LampSeconds = 70.0f,
			WalkerCount = 2,
			WandererCount = 1,
			RunnerCount = 0,
			OilCanisters = 0,
			Stones = 0,
			GradeS = 2900,
			GradeA = 2200,
			GradeB = 1600,
		},
		new LevelConfig
		{
			Level = 3,
			ArtifactsPlaced = 4,
			ArtifactsRequired = 3,
			ArtifactRadiusTiles = 7.0f,
			LampSeconds = 90.0f,
			WalkerCount = 2,
			WandererCount = 2,
			RunnerCount = 1,
			OilCanisters = 1,
			Stones = 2,
			GradeS = 4200,
			GradeA = 3300,
			GradeB = 2500,
		},
	};

	public static int Clamp(int level)
	{
		if (level < MinLevel)
			return MinLevel;

		return level > MaxLevel ? MaxLevel : level;
	}

	public static LevelConfig Get(int level)
	{
		return Levels[Clamp(level) - MinLevel];
	}

	public static string Grade(int level, int score)
	{
		LevelConfig config = Get(level);

		if (score >= config.GradeS)
			return "S";
		if (score >= config.GradeA)
			return "A";
		if (score >= config.GradeB)
			return "B";

		return "C";
	}
}

public static class ScoreRules
{
	public const int BasePerLevel = 500;
	public const int PerArtifact = 300;
	public const int PerLampSecond = 10;
	public const int SilentBonus = 400;
	public const int EvasionBonus = 250;

	public static int Total(int level, float weightedArtifacts, float lampRemaining, bool usedRun,
		int evasions)
	{
		int score = BasePerLevel * LevelTable.Clamp(level);
		score += UnityEngine.Mathf.RoundToInt(PerArtifact * UnityEngine.Mathf.Max(0.0f, weightedArtifacts));
		score += PerLampSecond * UnityEngine.Mathf.FloorToInt(UnityEngine.Mathf.Max(0.0f, lampRemaining));

		if (usedRun == false)
			score += SilentBonus;

		score += EvasionBonus * UnityEngine.Mathf.Max(0, evasions);
		return score;
	}
}
