using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameOver : UI_Popup
{
	const string ArtDir = "Art/UI/Game_Over_Screen/";

	const float HoldSeconds = 0.3f;
	const float SnuffSeconds = 0.5f;
	const int FlickerCount = 5;
	const float FlickerStep = 0.06f;
	const float SheetFadeSeconds = 0.25f;

	const float SnuffedDim = 0.2f;
	const float SettledDim = 0.62f;

	Image _dim;
	CanvasGroup _sheet;

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

		_dim = Stretch("Dim").gameObject.AddComponent<Image>();
		_dim.color = new Color(0, 0, 0, 0);
		_dim.raycastTarget = false;

		BuildSheet();

		Time.timeScale = 0;
		StartCoroutine(Play());
	}

	RectTransform Stretch(string name)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(transform, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
		return rect;
	}

	void BuildSheet()
	{
		RectTransform sheet = Stretch("Sheet");
		_sheet = sheet.gameObject.AddComponent<CanvasGroup>();
		_sheet.alpha = 0.0f;
		_sheet.interactable = false;
		_sheet.blocksRaycasts = false;

		Picture(sheet, "Your Dead Title", new Vector2(0.5f, 0.70f), 860.0f);
		BuildScore(sheet);
		Picture(sheet, "Play Again", new Vector2(0.5f, 0.34f), 260.0f);
		Choice(sheet, "Yes button", new Vector2(0.43f, 0.24f), 170.0f, Retry);
		Choice(sheet, "No button", new Vector2(0.57f, 0.24f), 135.0f, Quit);
	}

	void BuildScore(RectTransform sheet)
	{
		Image plank = Picture(sheet, "wooden_planks", new Vector2(0.5f, 0.5f), 300.0f);

		GameObject go = new GameObject("Score", typeof(RectTransform), typeof(Text));
		go.transform.SetParent(plank != null ? plank.rectTransform : sheet, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = plank != null ? Vector2.zero : new Vector2(0.5f, 0.5f);
		rect.anchorMax = plank != null ? Vector2.one : new Vector2(0.5f, 0.5f);
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
		if (plank == null)
			rect.sizeDelta = new Vector2(400, 100);

		Text score = go.GetComponent<Text>();
		score.text = Managers.Game.LastScore.ToString();
		score.font = KoreanFont.Font;
		score.fontSize = 48;
		score.alignment = TextAnchor.MiddleCenter;
		score.color = new Color(0.24f, 0.16f, 0.09f);
	}

	Image Picture(RectTransform parent, string file, Vector2 anchor, float width)
	{
		Sprite sprite = Resources.Load<Sprite>(ArtDir + file);
		if (sprite == null)
		{
			Debug.LogWarning($"UI_GameOver: Resources/{ArtDir}{file} 로드 실패");
			return null;
		}

		GameObject go = new GameObject(file, typeof(RectTransform), typeof(Image));
		go.transform.SetParent(parent, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = rect.anchorMax = anchor;
		rect.sizeDelta = new Vector2(
			width, width * sprite.rect.height / Mathf.Max(1.0f, sprite.rect.width));

		Image image = go.GetComponent<Image>();
		image.sprite = sprite;
		image.raycastTarget = false;
		return image;
	}

	void Choice(RectTransform parent, string file, Vector2 anchor, float width,
		UnityEngine.Events.UnityAction onClick)
	{
		Image image = Picture(parent, file, anchor, width);
		if (image == null)
			return;

		image.raycastTarget = true;

		Button button = image.gameObject.AddComponent<Button>();
		button.targetGraphic = image;
		button.onClick.AddListener(onClick);

		ColorBlock colors = button.colors;
		colors.normalColor = new Color(0.78f, 0.78f, 0.78f);
		colors.highlightedColor = Color.white;
		colors.pressedColor = new Color(0.6f, 0.55f, 0.4f);
		colors.fadeDuration = 0.05f;
		button.colors = colors;
	}

	IEnumerator Play()
	{
		yield return new WaitForSecondsRealtime(HoldSeconds);

		Lamp lamp = FindFirstObjectByType<Lamp>();

		for (float t = 0.0f; t < SnuffSeconds; t += Time.unscaledDeltaTime)
		{
			float k = t / SnuffSeconds;

			if (lamp != null)
				lamp.SnuffTo(1.0f - k);

			_dim.color = new Color(0, 0, 0, SnuffedDim * k);
			yield return null;
		}

		if (lamp != null)
			lamp.SnuffTo(0.0f);

		for (int i = 0; i < FlickerCount; i++)
		{
			_dim.color = new Color(0, 0, 0, SnuffedDim);
			yield return new WaitForSecondsRealtime(FlickerStep * 0.4f);
			_dim.color = new Color(0, 0, 0, SettledDim);
			yield return new WaitForSecondsRealtime(FlickerStep * 0.6f);
		}

		_dim.color = new Color(0, 0, 0, SettledDim);
		_sheet.interactable = true;
		_sheet.blocksRaycasts = true;

		for (float t = 0.0f; t < SheetFadeSeconds; t += Time.unscaledDeltaTime)
		{
			_sheet.alpha = t / SheetFadeSeconds;
			yield return null;
		}

		_sheet.alpha = 1.0f;
	}

	void Retry()
	{
		Time.timeScale = 1;
		Managers.Scene.LoadScene(Define.Scene.InGame);
	}

	void Quit()
	{
		Time.timeScale = 1;
		Managers.Scene.LoadScene(Define.Scene.Title);
	}
}
