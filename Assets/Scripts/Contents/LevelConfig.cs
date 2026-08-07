public class LevelConfig
{
	public int Level;
	public int ArtifactsPlaced;
	public int ArtifactsRequired;
	public float ArtifactRadiusTiles;
	public float LampSeconds;
	public float DeadlineSeconds;
	public float RitualSeconds;
	public int YokaiCount;
	public int WalkerCount;
	public int WandererCount;
	public int RunnerCount;
	public int OilCanisters;
	public int Stones;

	public int EnemyCount
	{
		get { return YokaiCount + WalkerCount + WandererCount + RunnerCount; }
	}
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
			ArtifactsRequired = 1,
			DeadlineSeconds = 120.0f,
			ArtifactRadiusTiles = 12.0f,
			LampSeconds = 60.0f,
			RitualSeconds = 6.0f,
			YokaiCount = 1,
			WalkerCount = 0,
			WandererCount = 0,
			RunnerCount = 0,
			OilCanisters = 0,
			Stones = 0,
		},
		new LevelConfig
		{
			Level = 2,
			ArtifactsPlaced = 3,
			ArtifactsRequired = 2,
			DeadlineSeconds = 180.0f,
			ArtifactRadiusTiles = 9.0f,
			LampSeconds = 70.0f,
			RitualSeconds = 7.0f,
			YokaiCount = 1,
			WalkerCount = 1,
			WandererCount = 0,
			RunnerCount = 0,
			OilCanisters = 0,
			Stones = 0,
		},
		new LevelConfig
		{
			Level = 3,
			ArtifactsPlaced = 4,
			ArtifactsRequired = 3,
			DeadlineSeconds = 240.0f,
			ArtifactRadiusTiles = 7.0f,
			LampSeconds = 90.0f,
			RitualSeconds = 8.0f,
			YokaiCount = 1,
			WalkerCount = 1,
			WandererCount = 1,
			RunnerCount = 1,
			OilCanisters = 1,
			Stones = 2,
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
}
