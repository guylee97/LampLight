using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Result : UI_Popup
{
	public const string ClearClip = "level_clear";
	public const string StampClip = "rank_stamp";
	public const string DefeatClip = "death_contact";

	bool _soundPlayed;

	enum Buttons
	{
		RetryButton,
		TitleButton,
	}

	enum Texts
	{
		ResultTitleText,
		ResultDetailText,
	}

	Define.StageResult _result;
	int _collected;
	int _required;
	bool _ready;
	Image _badge;

	public void Setup(Define.StageResult result, int collected, int required)
	{
		_result = result;
		_collected = collected;
		_required = required;

		Apply();
	}

	public override void Init()
	{
		base.Init();

		Bind<Button>(typeof(Buttons));
		Bind<Text>(typeof(Texts));

		GetButton((int)Buttons.RetryButton).gameObject.BindEvent(OnRetry);
		GetButton((int)Buttons.TitleButton).gameObject.BindEvent(OnTitle);

		PressAnyKeyPrompt.Attach(transform, "PRESS ANY KEY", 0.14f, 38);
		PressAnyKeyPrompt.Attach(transform, "ESC \uD0C0\uC774\uD2C0\uB85C", 0.075f, 24);

		_ready = true;
		BuildBadge();
		Apply();
	}

	void BuildBadge()
	{
		Text title = GetText((int)Texts.ResultTitleText);
		if (title == null)
			return;

		GameObject go = new GameObject("RankBadge");
		go.transform.SetParent(title.transform.parent, false);
		go.transform.SetAsFirstSibling();

		_badge = go.AddComponent<Image>();
		_badge.raycastTarget = false;
		_badge.preserveAspect = true;

		RectTransform rect = _badge.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 1.0f);
		rect.anchorMax = new Vector2(0.5f, 1.0f);
		rect.pivot = new Vector2(0.5f, 1.0f);
		rect.anchoredPosition = new Vector2(0.0f, -18.0f);
		rect.sizeDelta = new Vector2(88.0f, 88.0f);
	}

	void Apply()
	{
		if (_ready == false)
			return;

		bool cleared = _result == Define.StageResult.Cleared;

		if (_soundPlayed == false)
		{
			_soundPlayed = true;
			Managers.Sound.PlayOptional(cleared ? ClearClip : DefeatClip, Define.Sound.UI);
		}

		if (_badge != null)
			_badge.gameObject.SetActive(false);

		GameManagerEx game = Managers.Game;

		Text title = GetText((int)Texts.ResultTitleText);
		if (title != null)
			title.text = cleared ? $"{game.CurrentLevel}층 봉인 완료" : "붙잡혔다";

		Text detail = GetText((int)Texts.ResultDetailText);
		if (detail == null)
			return;

		if (cleared == false)
		{
			detail.text = $"유물  {_collected} / {_required}";
			return;
		}

		detail.text = game.HasNextLevel
			? "더 깊은 곳이 남아 있다"
			: "이 절의 모든 봉인을 마쳤다";
	}

	void OnRetry(PointerEventData data)
	{
		if (_result == Define.StageResult.Cleared)
			Managers.Game.AdvanceLevel();

		Managers.Scene.LoadScene(Define.Scene.InGame);
	}

	void OnTitle(PointerEventData data)
	{
		Managers.Scene.LoadScene(Define.Scene.Title);
	}

	void Update()
	{
		if (_ready == false)
			return;

		if (AnyKey.EscapeDown)
		{
			_ready = false;
			OnTitle(null);
			return;
		}

		if (AnyKey.Down)
		{
			_ready = false;
			OnRetry(null);
		}
	}
}
