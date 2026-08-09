using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Result : UI_Popup
{
	public const string ClearClip = "level_clear";
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

		PressAnyKeyPrompt.Attach(
			transform, PressAnyKeyPrompt.PressAnyKeyArt, 0.145f, 480.0f);
		PressAnyKeyPrompt.Attach(
			transform, PressAnyKeyPrompt.EscTitleArt, 0.075f, 230.0f);

		_ready = true;
		Apply();
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

		GameManagerEx game = Managers.Game;
		bool finalClear = cleared && game.HasNextLevel == false;

		Text title = GetText((int)Texts.ResultTitleText);
		if (title != null)
			title.text = finalClear ? "탈출 성공" : cleared ? $"{game.CurrentLevel}층 봉인 완료" : "붙잡혔다";

		Button retry = GetButton((int)Buttons.RetryButton);
		Button titleButton = GetButton((int)Buttons.TitleButton);
		if (retry != null)
			retry.gameObject.SetActive(finalClear == false);

		if (finalClear && titleButton != null)
		{
			RectTransform rect = titleButton.GetComponent<RectTransform>();
			if (rect != null)
				rect.anchoredPosition = new Vector2(0.0f, rect.anchoredPosition.y);
		}

		Text detail = GetText((int)Texts.ResultDetailText);
		if (detail == null)
			return;

		if (cleared == false)
		{
			detail.text = $"공양물  {_collected} / {_required}";
			return;
		}

		if (game.HasNextLevel)
		{
			detail.text = "더 깊은 곳이 남아 있다";
			return;
		}

		// 마지막 전각을 닫은 뒤에만 나오는 한 줄. 앞의 두 대사를 회수한다 —
		// "불이 꺼지면 걷는 게 너인지도 모르게 된다", "복도에서 뛰어다니는 게 걔들이다".
		DialogueBeat ending = DialogueTable.Book.ending;
		detail.text = ending != null && ending.lines != null && ending.lines.Length > 0
			? string.Join("\n", ending.lines)
			: "괴물들을 모두 봉인했다.\n당신은 마침내 신전을 빠져나왔다.";
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

			if (_result == Define.StageResult.Cleared && Managers.Game.HasNextLevel == false)
				OnTitle(null);
			else
				OnRetry(null);
		}
	}
}
