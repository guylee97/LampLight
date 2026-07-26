using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_MainScreen : UI_Scene
{
	enum Buttons
	{
		StartButton
	}

	public override void Init()
	{
		base.Init();

		Bind<Button>(typeof(Buttons));
		GetButton((int)Buttons.StartButton).gameObject.BindEvent(OnStartButtonClicked);
	}

	void OnStartButtonClicked(PointerEventData data)
	{
		Managers.Scene.LoadScene(Define.Scene.InGame);
	}
}
