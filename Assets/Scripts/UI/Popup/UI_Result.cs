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

		Text title = GetText((int)Texts.ResultTitleText);
		if (title != null)
			title.text = cleared ? $"{game.CurrentLevel}층 봉인 완료" : "붙잡혔다";

		Text detail = GetText((int)Texts.ResultDetailText);
		if (detail == null)
			return;

		if (cleared == false)
		{
			detail.text = $"공양물  {_collected} / {_required}";
			return;
		}

		detail.text = game.HasNextLevel
			? "더 깊은 곳이 남아 있다"
			: "이 신전의 모든 봉인을 마쳤다";
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
