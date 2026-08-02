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
		{
			_badge.gameObject.SetActive(cleared);

			if (cleared)
			{
				_badge.sprite = RankBadge.Get(Managers.Game.LastGrade);
				Managers.Sound.PlayOptional(StampClip, Define.Sound.UI);
			}
		}
		GameManagerEx game = Managers.Game;

		Text title = GetText((int)Texts.ResultTitleText);
		if (title != null)
			title.text = cleared ? $"LEVEL {game.CurrentLevel} 탈출  ·  {game.LastGrade}" : "붙잡혔다";

		Text detail = GetText((int)Texts.ResultDetailText);
		if (detail == null)
			return;

		if (cleared == false)
		{
			detail.text = $"유물  {_collected} / {_required}";
			return;
		}

		string next = game.HasNextLevel ? "\n[다시하기]로 다음 레벨" : "\n최종 레벨 클리어";
		string silent = game.UsedRun ? "" : $"\n무소음 보너스  +{ScoreRules.SilentBonus}";
		string evasion = game.RunnerEvasions > 0
			? $"\n회피 보너스  +{ScoreRules.EvasionBonus * game.RunnerEvasions}"
			: "";

		detail.text = $"유물  {_collected} / {_required}\n점수  {game.LastScore}{silent}{evasion}{next}";
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
}
