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
	public string ScareSheet;
	public string ScreamClip;
	public string ChaseClip;

	public float FaceHoldSeconds;
	public UnityEngine.Color Tint;
}

public static class YokaiTable
{
	static readonly YokaiSpec[] Specs =
	{
		// 1전각 — 허리 아래가 연기로 흩어진 상반신. 낮아서 늦게 보인다.
		new YokaiSpec
		{
			Key = "banshin",
			CharacterKey = "banshin",
			Label = "반신",
			PatrolSpeed = 1.2f,
			ChaseSpeed = 3.2f,
			SearchSpeed = 2.0f,
			SightRange = 9.0f,
			HearingScale = 0.5f,
			FaceArt = "jumpscare_banshin",
			ScareSheet = "scare_banshin_sheet",
			ScreamClip = "Monster/깜짝놀라게/freesound_community-growl-2-84549",
			ChaseClip = "chase_stinger",
			FaceHoldSeconds = 0.34f,
			Tint = new UnityEngine.Color(0.86f, 0.86f, 0.90f, 1.0f),
		},
		// 2전각 — 관절이 거꾸로 꺾여 기는 몸. 빠른데 소리가 크다.
		new YokaiSpec
		{
			Key = "yeokgol",
			CharacterKey = "yeokgol",
			Label = "역골",
			PatrolSpeed = 1.8f,
			ChaseSpeed = 4.0f,
			SearchSpeed = 2.6f,
			SightRange = 7.5f,
			HearingScale = 0.8f,
			FaceArt = "jumpscare_yeokgol",
			ScareSheet = "scare_yeokgol_sheet",
			ScreamClip = "Monster/깜짝놀라게/dragon-studio-beast-growl-494304",
			ChaseClip = "chase_stinger",
			FaceHoldSeconds = 0.46f,
			Tint = new UnityEngine.Color(0.70f, 0.76f, 0.74f, 1.0f),
		},
		// 3전각 — 검은 상복의 키 큰 것. 느린데 절대 안 멈춘다.
		new YokaiSpec
		{
			Key = "sangju",
			CharacterKey = "sangju",
			Label = "상주",
			PatrolSpeed = 1.5f,
			ChaseSpeed = 4.6f,
			SearchSpeed = 3.0f,
			SightRange = 6.5f,
			HearingScale = 0.95f,
			FaceArt = "jumpscare_sangju",
			ScareSheet = "scare_sangju_sheet",
			ScreamClip = "Monster/깜짝놀라게/dragon-studio-monster-growl-390285",
			ChaseClip = "chase_stinger",
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
