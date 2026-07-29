using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Result : UI_Popup
{
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

		_ready = true;
		Apply();
	}

	void Apply()
	{
		if (_ready == false)
			return;

		Text title = GetText((int)Texts.ResultTitleText);
		if (title != null)
			title.text = _result == Define.StageResult.Cleared ? "탈출 성공" : "붙잡혔다";

		Text detail = GetText((int)Texts.ResultDetailText);
		if (detail != null)
			detail.text = $"유물  {_collected} / {_required}";
	}

	void OnRetry(PointerEventData data)
	{
		Managers.Scene.LoadScene(Define.Scene.InGame);
	}

	void OnTitle(PointerEventData data)
	{
		Managers.Scene.LoadScene(Define.Scene.Title);
	}
}
