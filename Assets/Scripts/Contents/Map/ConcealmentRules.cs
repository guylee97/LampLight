using UnityEngine;

public static class ConcealmentRules
{
	public const int Max = 2;

	public static int Clamp(int level)
	{
		return Mathf.Clamp(level, 0, Max);
	}

	public static float HoldSeconds(int level)
	{
		switch (Clamp(level))
		{
			case 1: return 2.0f;
			case 2: return 3.0f;
			default: return 0.0f;
		}
	}

	public static float RadiusScale(int level)
	{
		switch (Clamp(level))
		{
			case 1: return 0.75f;
			case 2: return 0.55f;
			default: return 1.0f;
		}
	}

	public static float NoiseRadius(int level)
	{
		switch (Clamp(level))
		{
			case 1: return 7.0f;
			case 2: return 10.0f;
			default: return 6.0f;
		}
	}

	public static float ScoreWeight(int level)
	{
		switch (Clamp(level))
		{
			case 1: return 1.3f;
			case 2: return 1.6f;
			default: return 1.0f;
		}
	}

	public static int ForLevel(int level, int index)
	{
		if (level <= 1)
			return 0;

		if (level == 2)
			return index % 2 == 0 ? 0 : 1;

		return index % 2 == 0 ? 1 : 2;
	}
}
