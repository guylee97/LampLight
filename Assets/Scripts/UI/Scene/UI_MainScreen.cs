using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_MainScreen : UI_Scene
{
	enum Buttons
	{
		StartButton
	}

	PressAnyKeyPrompt _prompt;
	bool _ready;

	void Update()
	{
		if (_ready == false || AnyKey.Down == false)
			return;

		_ready = false;
		OnStartButtonClicked(null);
	}

	public override void Init()
	{
		base.Init();

		Bind<Button>(typeof(Buttons));
		Button start = GetButton((int)Buttons.StartButton);
		start.gameObject.BindEvent(OnStartButtonClicked);
		ApplyArtwork(start);
		_prompt = PressAnyKeyPrompt.Attach(
			transform, PressAnyKeyPrompt.PressAnyKeyArt, 0.135f, 560.0f);
		_prompt.gameObject.SetActive(false);
		Managers.Sound.PlayOptional(
			"Title_Background_Music/👍Title_Background_Mixing",
			Define.Sound.Ambient,
			volume: 8.0f,
			loop: true);
		StartCoroutine(ShowSoundNotice(start));
	}

	void ApplyArtwork(Button start)
	{
		Sprite background = LoadSprite("Art/UI/Title screen/Title");
		if (background != null)
		{
			GameObject go = new GameObject("TitleBackground", typeof(RectTransform), typeof(Image));
			go.transform.SetParent(transform, false);
			go.transform.SetAsFirstSibling();
			RectTransform rect = go.GetComponent<RectTransform>();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			Image image = go.GetComponent<Image>();
			image.sprite = background;
			image.raycastTarget = false;
		}

		Sprite logo = LoadSprite("Art/UI/Title screen/Lantern Title");
		if (logo != null)
		{
			GameObject go = new GameObject("TitleLogo", typeof(RectTransform), typeof(Image));
			go.transform.SetParent(transform, false);
			RectTransform rect = go.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0.5f, 0.76f);
			rect.anchorMax = new Vector2(0.5f, 0.76f);
			rect.anchoredPosition = Vector2.zero;
			rect.sizeDelta = new Vector2(920.0f, 620.0f);
			Image image = go.GetComponent<Image>();
			image.sprite = logo;
			image.preserveAspect = true;
			image.raycastTarget = false;
		}

		Sprite buttonSprite = LoadSprite("Art/UI/Title screen/Start button");
		Image buttonImage = start == null ? null : start.GetComponent<Image>();
		if (buttonImage != null && buttonSprite != null)
		{
			RectTransform rect = start.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0.5f, 0.24f);
			rect.anchorMax = new Vector2(0.5f, 0.24f);
			rect.anchoredPosition = Vector2.zero;
			rect.sizeDelta = new Vector2(360.0f, 150.0f);

			buttonImage.sprite = buttonSprite;
			buttonImage.preserveAspect = true;
			HideButtonLabel(start);
		}
	}

	static void HideButtonLabel(Button start)
	{
		foreach (Graphic label in start.GetComponentsInChildren<Graphic>(true))
		{
			if (label.gameObject == start.gameObject || label is Image)
				continue;

			label.gameObject.SetActive(false);
		}
	}

	IEnumerator ShowSoundNotice(Button start)
	{
		if (start != null)
			start.interactable = false;

		GameObject splash = new GameObject(
			"SoundNoticeSplash",
			typeof(RectTransform),
			typeof(Image),
			typeof(CanvasGroup));
		splash.transform.SetParent(transform, false);
		splash.transform.SetAsLastSibling();

		RectTransform splashRect = splash.GetComponent<RectTransform>();
		splashRect.anchorMin = Vector2.zero;
		splashRect.anchorMax = Vector2.one;
		splashRect.offsetMin = Vector2.zero;
		splashRect.offsetMax = Vector2.zero;

		Image black = splash.GetComponent<Image>();
		black.color = Color.black;
		black.raycastTarget = true;

		Sprite notice = LoadSprite("Art/UI/Title screen/SoundNotice");
		if (notice != null)
		{
			GameObject noticeObject = new GameObject(
				"SoundNotice",
				typeof(RectTransform),
				typeof(Image));
			noticeObject.transform.SetParent(splash.transform, false);

			RectTransform noticeRect = noticeObject.GetComponent<RectTransform>();
			noticeRect.anchorMin = new Vector2(0.5f, 0.5f);
			noticeRect.anchorMax = new Vector2(0.5f, 0.5f);
			noticeRect.anchoredPosition = Vector2.zero;
			noticeRect.sizeDelta = new Vector2(1250.0f, 80.0f);

			Image noticeImage = noticeObject.GetComponent<Image>();
			noticeImage.sprite = notice;
			noticeImage.preserveAspect = true;
			noticeImage.raycastTarget = false;
		}

		yield return new WaitForSecondsRealtime(2.0f);

		if (_prompt != null)
			_prompt.gameObject.SetActive(true);

		_ready = true;

		CanvasGroup group = splash.GetComponent<CanvasGroup>();
		const float fadeSeconds = 0.6f;
		float elapsed = 0.0f;
		while (elapsed < fadeSeconds)
		{
			elapsed += Time.unscaledDeltaTime;
			group.alpha = 1.0f - Mathf.Clamp01(elapsed / fadeSeconds);
			yield return null;
		}

		Destroy(splash);
		if (start != null)
			start.interactable = true;
	}

	static Sprite LoadSprite(string path)
	{
		Sprite sprite = Resources.Load<Sprite>(path);
		if (sprite != null)
			return sprite;

		Sprite[] sprites = Resources.LoadAll<Sprite>(path);
		return sprites.Length == 0 ? null : sprites[0];
	}

	void OnStartButtonClicked(PointerEventData data)
	{
		Managers.Sound.PlayOptional(
			"UI_Click/freesound_community-door-lock-82542",
			Define.Sound.UI);
		Managers.Scene.LoadScene(Define.Scene.InGame);
	}
}
