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
	public string[] OfferingLines;
}

public static class YokaiTable
{
	static readonly YokaiSpec[] Specs =
	{
		// 1전각 — 관절이 거꾸로 꺾여 기는 몸. 빠른데 눈과 귀가 어둡다.
		// 보이면 위험하다는 것만 가르치는 첫 요괴다.
		new YokaiSpec
		{
			Key = "yeokgol",
			CharacterKey = "yeokgol",
			Label = "역골",
			PatrolSpeed = 1.6f,
			ChaseSpeed = 3.6f,
			SearchSpeed = 2.2f,
			SightRange = 7.0f,
			HearingScale = 0.5f,
			FaceArt = "jumpscare_yeokgol",
			ScareSheet = "scare_yeokgol_sheet",
			ScreamClip = "Monster/깜짝놀라게/dragon-studio-beast-growl-494304",
			ChaseClip = "chase_stinger",
			FaceHoldSeconds = 0.46f,
			Tint = new UnityEngine.Color(0.70f, 0.76f, 0.74f, 1.0f),
			OfferingLines = new[]
			{
				"뚝, 하고 뭐가 꺾이는 소리가 났어.",
				"방금 뭔가 벽을 타고 지나갔어.",
			},
		},
		// 2전각 — 검은 상복의 키 큰 것. 멀리서 보고, 한번 붙으면 안 놓는다.
		new YokaiSpec
		{
			Key = "sangju",
			CharacterKey = "sangju",
			Label = "상주",
			PatrolSpeed = 1.5f,
			ChaseSpeed = 4.2f,
			SearchSpeed = 2.8f,
			SightRange = 8.5f,
			HearingScale = 0.75f,
			FaceArt = "jumpscare_sangju",
			ScareSheet = "scare_sangju_sheet",
			ScreamClip = "Monster/깜짝놀라게/dragon-studio-monster-growl-390285",
			ChaseClip = "chase_stinger",
			FaceHoldSeconds = 0.28f,
			Tint = new UnityEngine.Color(0.95f, 0.80f, 0.78f, 1.0f),
			OfferingLines = new[]
			{
				"천 스치는 소리가 계속 따라와.",
				"아까 저기에 뭐가 서 있었던 것 같은데.",
				"멈추질 않아. 계속 따라와.",
			},
		},
		// 3전각 — 다리 대신 뿌리로 바닥을 끌고 오는 돌덩이. 거의 못 보는 대신
		// 발밑 울림을 전부 읽는다. 가슴에 박힌 불이 등불처럼 보인다.
		new YokaiSpec
		{
			Key = "seoksin",
			CharacterKey = "seoksin",
			Label = "석신",
			PatrolSpeed = 1.1f,
			ChaseSpeed = 4.8f,
			SearchSpeed = 3.2f,
			SightRange = 6.0f,
			HearingScale = 1.0f,
			FaceArt = "jumpscare_seoksin",
			ScareSheet = "scare_seoksin_sheet",
			ScreamClip = "Monster/깜짝놀라게/freesound_community-growl-2-84549",
			ChaseClip = "chase_stinger",
			FaceHoldSeconds = 0.40f,
			Tint = new UnityEngine.Color(0.96f, 0.87f, 0.80f, 1.0f),
			OfferingLines = new[]
			{
				"바닥이 조금 울렸어.",
				"아까 그 석상, 저기 있었나?",
				"돌 긁히는 소리가 뒤에서 나.",
				"불빛이 하나 더 있어. 내 등불 말고.",
			},
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
