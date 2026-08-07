using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
	public const string HintClip = "tutorial_bell";
	public const string CurtainClip = "tutorial_step";

	public const string DoneKey = "onelantern.tutorialDone.v1";

	static readonly string[] IntroLines =
	{
		"불이 꺼지기 전에, 봉인을 마쳐라.",
	};

	[SerializeField]
	float _lineSeconds = 2.4f;

	[SerializeField]
	float _hintSeconds = 4.0f;

	Text _text;
	Image _curtain;
	PlayerController _player;
	StageProgress _progress;
	MapObjectPlacer _placer;

	public static bool IsDone { get { return PlayerPrefs.GetInt(DoneKey, 0) == 1; } }

	public static void MarkDone()
	{
		PlayerPrefs.SetInt(DoneKey, 1);
		PlayerPrefs.Save();
	}

	public static void Reset()
	{
		PlayerPrefs.DeleteKey(DoneKey);
		PlayerPrefs.Save();
	}

	void Start()
	{
		Build();
		_player = FindFirstObjectByType<PlayerController>();
		_progress = FindFirstObjectByType<StageProgress>();
		_placer = FindFirstObjectByType<MapObjectPlacer>();

		bool skip = Application.isBatchMode
			|| Managers.Game.CurrentLevel != LevelTable.MinLevel
			|| IsDone;

		if (skip)
			HideCurtain();
		else
			StartCoroutine(RunIntro());
	}

	void Build()
	{
		GameObject canvasGo = new GameObject("TutorialCanvas");
		canvasGo.transform.SetParent(transform, false);

		Canvas canvas = canvasGo.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 600;

		CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

		GameObject curtainGo = new GameObject("Curtain");
		curtainGo.transform.SetParent(canvasGo.transform, false);
		_curtain = curtainGo.AddComponent<Image>();
		_curtain.color = new Color(0.016f, 0.024f, 0.039f, 1.0f);
		_curtain.raycastTarget = false;
		Stretch(_curtain.rectTransform);

		GameObject textGo = new GameObject("Line");
		textGo.transform.SetParent(canvasGo.transform, false);
		_text = textGo.AddComponent<Text>();
		_text.font = KoreanFont.Font != null
			? KoreanFont.Font
			: Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		_text.fontSize = 30;
		_text.alignment = TextAnchor.MiddleCenter;
		_text.color = new Color(0.90f, 0.76f, 0.53f, 1.0f);
		_text.raycastTarget = false;
		Stretch(_text.rectTransform);
	}

	static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	IEnumerator RunIntro()
	{
		Managers.Game.SetPaused(true);

		foreach (string line in IntroLines)
		{
			_text.text = line;
			Managers.Sound.PlayOptional("text_type", Define.Sound.UI);
			yield return new WaitForSecondsRealtime(_lineSeconds);
		}

		Managers.Sound.PlayOptional("lantern_ignite", Define.Sound.Ambient);
		_text.text = string.Empty;

		float fade = 0.0f;
		while (fade < 1.0f)
		{
			fade += Time.unscaledDeltaTime * 1.5f;
			Color c = _curtain.color;
			c.a = 1.0f - fade;
			_curtain.color = c;
			yield return null;
		}

		HideCurtain();
		Managers.Game.SetPaused(false);
		MarkDone();

		yield return Hint("WASD 이동   ·   F 등불   ·   E 상호작용");
	}

	void HideCurtain()
	{
		Managers.Sound.PlayOptional(CurtainClip, Define.Sound.Guide);

		if (_curtain != null)
			_curtain.gameObject.SetActive(false);
	}

	IEnumerator Hint(string message)
	{
		_text.text = message;
		yield return new WaitForSeconds(_hintSeconds);

		if (_text.text == message)
			_text.text = string.Empty;
	}

}
