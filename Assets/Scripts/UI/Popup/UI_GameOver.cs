using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameOver : UI_Popup
{
	const string ArtDir = "Art/UI/Game_Over_Screen/";
	const string FaceSprite = "jumpscare_mask";

	public const string ScreamClip = "jumpscare_scream";
	public const string ScreamFallbackClip = "moster growl (4)";

	const float SilenceSeconds = 0.15f;
	const float SlamSeconds = 0.10f;
	const float HoldSeconds = 0.55f;
	const float BlackoutSeconds = 0.30f;
	const float SheetDelaySeconds = 0.35f;
	const float SheetFadeSeconds = 0.25f;

	const int FlickerCount = 6;
	const float FlickerOnSeconds = 0.045f;
	const float FlickerOffSeconds = 0.075f;
	const float FlickerDim = 0.86f;

	const float HitstopScale = 0.03f;
	const float SlamStartScale = 1.9f;
	const float FocusZoom = 0.55f;

	Image _face;
	Image _flash;
	Image _dim;
	CanvasGroup _sheet;
	CameraController _camera;
	float _listenerVolume = 1.0f;

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

		BuildFace();

		_flash = Stretch("Flash").gameObject.AddComponent<Image>();
		_flash.color = new Color(0.55f, 0.02f, 0.02f, 0.0f);
		_flash.raycastTarget = false;

		BuildSheet();

		_camera = FindFirstObjectByType<CameraController>();
		_listenerVolume = AudioListener.volume;

		StartCoroutine(Play());
	}

	RectTransform Stretch(string childName)
	{
		GameObject go = new GameObject(childName, typeof(RectTransform));
		go.transform.SetParent(transform, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
		return rect;
	}

	void BuildFace()
	{
		RectTransform rect = Stretch("Jumpscare");
		_face = rect.gameObject.AddComponent<Image>();
		_face.sprite = Resources.Load<Sprite>(ArtDir + FaceSprite);
		_face.preserveAspect = false;
		_face.raycastTarget = false;
		_face.color = new Color(1, 1, 1, 0);
		_face.enabled = _face.sprite != null;
	}

	void BuildSheet()
	{
		RectTransform sheet = Stretch("Sheet");
		_sheet = sheet.gameObject.AddComponent<CanvasGroup>();
		_sheet.alpha = 0.0f;
		_sheet.interactable = false;
		_sheet.blocksRaycasts = false;

		Picture(sheet, "Your Dead Title", new Vector2(0.5f, 0.668f), 1434.0f);
		BuildScore(sheet);
		Picture(sheet, "Play Again", new Vector2(0.5f, 0.278f), 212.0f);
		Choice(sheet, "Yes button", new Vector2(0.4208f, 0.2046f), 184.0f, Retry);
		Choice(sheet, "No button", new Vector2(0.5898f, 0.2046f), 143.0f, Quit);
	}

	void BuildScore(RectTransform sheet)
	{
		Image plank = Picture(sheet, "wooden_planks", new Vector2(0.5f, 0.419f), 386.0f);

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
		yield return Hitstop();
		yield return Slam();
		yield return Flicker();
		yield return Blackout();
		yield return RevealSheet();
	}

	IEnumerator Flicker()
	{
		if (_face.sprite != null)
			_face.enabled = false;

		for (int i = 0; i < FlickerCount; i++)
		{
			_dim.color = new Color(0, 0, 0, FlickerDim);
			yield return new WaitForSecondsRealtime(FlickerOffSeconds);

			_dim.color = new Color(0, 0, 0, 0.0f);
			yield return new WaitForSecondsRealtime(FlickerOnSeconds);
		}

		_dim.color = new Color(0, 0, 0, FlickerDim);
	}

	IEnumerator Hitstop()
	{
		Time.timeScale = HitstopScale;
		AudioListener.volume = 0.0f;

		Transform catcher = Managers.Game.Catcher;

		if (_camera != null)
		{
			if (catcher != null)
				_camera.FocusOn(catcher, 0.85f, 26.0f);

			_camera.ZoomTo(FocusZoom, 4.5f);
			_camera.Shake(0.32f, SilenceSeconds + SlamSeconds + HoldSeconds);
		}

		yield return new WaitForSecondsRealtime(SilenceSeconds);
	}

	IEnumerator Slam()
	{
		Time.timeScale = 0.0f;
		AudioListener.volume = _listenerVolume;

		Managers.Sound.PlayOptional(ScreamClip, ScreamFallbackClip, Define.Sound.Threat);

		Lamp lamp = FindFirstObjectByType<Lamp>();
		if (lamp != null)
			lamp.SnuffTo(0.0f);

		if (_face.sprite == null)
		{
			_dim.color = new Color(0, 0, 0, 0.85f);
			yield break;
		}

		RectTransform faceRect = _face.rectTransform;

		for (float t = 0.0f; t < SlamSeconds; t += Time.unscaledDeltaTime)
		{
			float k = Mathf.Clamp01(t / SlamSeconds);
			float scale = Mathf.Lerp(SlamStartScale, 1.0f, k * k);

			faceRect.localScale = new Vector3(scale, scale, 1.0f);
			_face.color = new Color(1, 1, 1, Mathf.Clamp01(k * 2.5f));
			_flash.color = new Color(0.55f, 0.02f, 0.02f, 0.55f * (1.0f - k));
			yield return null;
		}

		faceRect.localScale = Vector3.one;
		_face.color = Color.white;
		_flash.color = new Color(0.55f, 0.02f, 0.02f, 0.0f);

		for (float t = 0.0f; t < HoldSeconds; t += Time.unscaledDeltaTime)
		{
			float jitter = 1.0f + 0.012f * Mathf.Sin(Time.unscaledTime * 60.0f);
			faceRect.localScale = new Vector3(jitter, jitter, 1.0f);
			yield return null;
		}
	}

	IEnumerator Blackout()
	{
		float from = _dim.color.a;

		for (float t = 0.0f; t < BlackoutSeconds; t += Time.unscaledDeltaTime)
		{
			float k = Mathf.Clamp01(t / BlackoutSeconds);
			_dim.color = new Color(0, 0, 0, Mathf.Lerp(from, 1.0f, k));
			yield return null;
		}

		_dim.color = Color.black;
		_face.enabled = false;

		yield return new WaitForSecondsRealtime(SheetDelaySeconds);
	}

	IEnumerator RevealSheet()
	{
		_dim.color = new Color(0, 0, 0, 0.82f);
		_sheet.interactable = true;
		_sheet.blocksRaycasts = true;

		for (float t = 0.0f; t < SheetFadeSeconds; t += Time.unscaledDeltaTime)
		{
			_sheet.alpha = t / SheetFadeSeconds;
			yield return null;
		}

		_sheet.alpha = 1.0f;
	}

	void Restore()
	{
		AudioListener.volume = _listenerVolume;
		Time.timeScale = 1;

		if (_camera != null)
			_camera.ResetEffects();
	}

	void Retry()
	{
		Restore();
		Managers.Scene.LoadScene(Define.Scene.InGame);
	}

	void Quit()
	{
		Restore();
		Managers.Scene.LoadScene(Define.Scene.Title);
	}
}
