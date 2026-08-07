public class YokaiSpec
{
	public string Key;
	public string CharacterKey;
	public string Label;

	public float PatrolSpeed;
	public float ChaseSpeed;
	public float SearchSpeed;
	public float SightRange;
	public float HearingScale;

	public string FaceArt;
	public string ScreamClip;
	public string ChaseClip;

	public float LungeScale;
	public float FaceHoldSeconds;
	public UnityEngine.Color Tint;
}

public static class YokaiTable
{
	static readonly YokaiSpec[] Specs =
	{
		new YokaiSpec
		{
			Key = "agwi",
			CharacterKey = "agwi",
			Label = "아귀",
			PatrolSpeed = 1.6f,
			ChaseSpeed = 3.4f,
			SearchSpeed = 2.2f,
			SightRange = 7.5f,
			HearingScale = 0.55f,
			FaceArt = "jumpscare_face",
			ScreamClip = "jumpscare_scream",
			ChaseClip = "chase_stinger",
			LungeScale = 3.1f,
			FaceHoldSeconds = 0.34f,
			Tint = new UnityEngine.Color(0.86f, 0.86f, 0.90f, 1.0f),
		},
		new YokaiSpec
		{
			Key = "monk",
			CharacterKey = "monk",
			Label = "목 없는 중",
			PatrolSpeed = 1.3f,
			ChaseSpeed = 3.9f,
			SearchSpeed = 2.6f,
			SightRange = 9.0f,
			HearingScale = 0.75f,
			FaceArt = "jumpscare_monk",
			ScreamClip = "jumpscare_scream",
			ChaseClip = "chase_stinger",
			LungeScale = 2.7f,
			FaceHoldSeconds = 0.46f,
			Tint = new UnityEngine.Color(0.70f, 0.76f, 0.74f, 1.0f),
		},
		new YokaiSpec
		{
			Key = "rakshasa",
			CharacterKey = "rakshasa",
			Label = "나찰",
			PatrolSpeed = 1.9f,
			ChaseSpeed = 4.6f,
			SearchSpeed = 3.0f,
			SightRange = 8.0f,
			HearingScale = 0.9f,
			FaceArt = "jumpscare_rakshasa",
			ScreamClip = "jumpscare_scream",
			ChaseClip = "chase_stinger",
			LungeScale = 3.6f,
			FaceHoldSeconds = 0.28f,
			Tint = new UnityEngine.Color(0.95f, 0.80f, 0.78f, 1.0f),
		},
	};

	public static YokaiSpec ForLevel(int level)
	{
		return Specs[LevelTable.Clamp(level) - LevelTable.MinLevel];
	}

	public static YokaiSpec Get(string key)
	{
		foreach (YokaiSpec spec in Specs)
		{
			if (spec.Key == key)
				return spec;
		}

		return Specs[0];
	}

	public static int Count { get { return Specs.Length; } }

	public static YokaiSpec At(int index)
	{
		return Specs[UnityEngine.Mathf.Clamp(index, 0, Specs.Length - 1)];
	}
}
