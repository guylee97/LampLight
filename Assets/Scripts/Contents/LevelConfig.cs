public class LevelConfig
{
	public int Level;
	public int ArtifactsPlaced;
	public int ArtifactsRequired;
	public float ArtifactRadiusTiles;
	public float LampSeconds;
	public float RitualSeconds;
	public int YokaiCount;
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
			ArtifactsRequired = 2,
			ArtifactRadiusTiles = 12.0f,
			LampSeconds = 60.0f,
			RitualSeconds = 6.0f,
			YokaiCount = 1,
		},
		new LevelConfig
		{
			Level = 2,
			ArtifactsPlaced = 3,
			ArtifactsRequired = 3,
			ArtifactRadiusTiles = 9.0f,
			LampSeconds = 90.0f,
			RitualSeconds = 7.0f,
			YokaiCount = 1,
		},
		new LevelConfig
		{
			Level = 3,
			ArtifactsPlaced = 4,
			ArtifactsRequired = 4,
			ArtifactRadiusTiles = 7.0f,
			LampSeconds = 125.0f,
			RitualSeconds = 8.0f,
			YokaiCount = 1,
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
