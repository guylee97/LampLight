using UnityEngine;
using UnityEngine.UI;

public class UI_GameOver : UI_Popup
{
	public override void Init()
	{
		base.Init();

		RectTransform root = GetComponent<RectTransform>();
		root.anchorMin = Vector2.zero;
		root.anchorMax = Vector2.one;
		root.offsetMin = Vector2.zero;
		root.offsetMax = Vector2.zero;
		root.localScale = Vector3.one;

		CanvasScaler scaler = gameObject.GetOrAddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);
		scaler.matchWidthOrHeight = 0.5f;
		gameObject.GetOrAddComponent<GraphicRaycaster>();

		GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
		background.transform.SetParent(transform, false);
		RectTransform backgroundRect = background.GetComponent<RectTransform>();
		backgroundRect.anchorMin = Vector2.zero;
		backgroundRect.anchorMax = Vector2.one;
		backgroundRect.offsetMin = Vector2.zero;
		backgroundRect.offsetMax = Vector2.zero;
		background.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

		GameObject title = new GameObject("Title", typeof(RectTransform), typeof(Text));
		title.transform.SetParent(background.transform, false);
		RectTransform titleRect = title.GetComponent<RectTransform>();
		titleRect.anchorMin = new Vector2(0.5f, 0.5f);
		titleRect.anchorMax = new Vector2(0.5f, 0.5f);
		titleRect.sizeDelta = new Vector2(900, 180);

		Text titleText = title.GetComponent<Text>();
		titleText.text = "GAME OVER";
		titleText.font = KoreanFont.Font;
		titleText.fontSize = 72;
		titleText.alignment = TextAnchor.MiddleCenter;
		titleText.color = Color.white;

		Time.timeScale = 0;
	}
}
