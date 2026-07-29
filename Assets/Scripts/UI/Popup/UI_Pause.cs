using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Pause : UI_Popup
{
	enum Buttons
	{
		ResumeButton,
		TitleButton,
	}

	public override void Init()
	{
		base.Init();

		Bind<Button>(typeof(Buttons));

		GetButton((int)Buttons.ResumeButton).gameObject.BindEvent(OnResume);
		GetButton((int)Buttons.TitleButton).gameObject.BindEvent(OnTitle);
	}

	void OnResume(PointerEventData data)
	{
		Managers.Game.SetPaused(false);
		ClosePopupUI();
	}

	void OnTitle(PointerEventData data)
	{
		Managers.Game.SetPaused(false);
		Managers.Scene.LoadScene(Define.Scene.Title);
	}
}
