using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameOver : UI_Popup
{
	const string ArtDir = "Art/UI/Game_Over_Screen/";
	const string FaceSprite = "jumpscare_mask";

	public const string ScreamClip = "jumpscare_scream";
	public const string ScreamFallbackClip = "moster growl (4)";

	const float SilenceSeconds = 0.10f;
	const float FaceSeconds = 0.34f;
	const float SettleSeconds = 0.28f;
	const float SheetFadeSeconds = 0.22f;

	const int FlickerCount = 3;
	const float FlickerOnSeconds = 0.03f;
	const float FlickerOffSeconds = 0.03f;
	const float FlickerDim = 0.88f;

	const float HitstopScale = 0.02f;
	const float JitterPixels = 0.014f;

	Image _face;
	Image _flash;
	Image _dim;
	CanvasGroup _sheet;
	CameraController _camera;
	PressAnyKeyPrompt _prompt;
	bool _acceptsInput;
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
		_prompt = PressAnyKeyPrompt.Attach(sheet, "PRESS ANY KEY \uB2E4\uC2DC", 0.30f, 40);
		PressAnyKeyPrompt.Attach(sheet, "ESC \uD0C0\uC774\uD2C0\uB85C", 0.225f, 26);
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

	IEnumerator Play()
	{
		yield return Hitstop();
		yield return Flicker();
		yield return Face();
		yield return Settle();
		yield return RevealSheet();
	}

	IEnumerator Hitstop()
	{
		Time.timeScale = HitstopScale;
		AudioListener.volume = 0.0f;

		if (_camera != null)
			_camera.Shake(0.28f, SilenceSeconds + FlickerSpan + FaceSeconds);

		yield return new WaitForSecondsRealtime(SilenceSeconds);
	}

	static float FlickerSpan
	{
		get { return FlickerCount * (FlickerOnSeconds + FlickerOffSeconds); }
	}

	IEnumerator Flicker()
	{
		Time.timeScale = 0.0f;

		for (int i = 0; i < FlickerCount; i++)
		{
			_dim.color = new Color(0, 0, 0, FlickerDim);
			yield return new WaitForSecondsRealtime(FlickerOffSeconds);

			_dim.color = new Color(0, 0, 0, 0.0f);
			yield return new WaitForSecondsRealtime(FlickerOnSeconds);
		}
	}

	IEnumerator Face()
	{
		AudioListener.volume = _listenerVolume;
		Managers.Sound.PlayOptional(ScreamClip, ScreamFallbackClip, Define.Sound.Threat);

		Lamp lamp = FindFirstObjectByType<Lamp>();
		if (lamp != null)
			lamp.SnuffTo(0.0f);

		if (_face.sprite == null)
		{
			_dim.color = new Color(0, 0, 0, 0.92f);
			yield return new WaitForSecondsRealtime(FaceSeconds);
			yield break;
		}

		_dim.color = new Color(0, 0, 0, 0.0f);
		_face.color = Color.white;
		_flash.color = new Color(0.55f, 0.02f, 0.02f, 0.5f);

		RectTransform faceRect = _face.rectTransform;
		RectTransform root = GetComponent<RectTransform>();
		float amplitude = root.rect.height * JitterPixels;

		for (float t = 0.0f; t < FaceSeconds; t += Time.unscaledDeltaTime)
		{
			float seed = Time.unscaledTime * 90.0f;
			faceRect.anchoredPosition = new Vector2(
				(Mathf.PerlinNoise(seed, 0.0f) * 2.0f - 1.0f) * amplitude,
				(Mathf.PerlinNoise(0.0f, seed) * 2.0f - 1.0f) * amplitude);

			_flash.color = new Color(
				0.55f, 0.02f, 0.02f, 0.5f * (1.0f - Ease.OutQuint(t / FaceSeconds)));
			yield return null;
		}
	}

	IEnumerator Settle()
	{
		_face.enabled = false;
		_flash.color = new Color(0.55f, 0.02f, 0.02f, 0.0f);
		_dim.color = Color.black;

		yield return new WaitForSecondsRealtime(SettleSeconds);
	}

	IEnumerator RevealSheet()
	{
		_dim.color = new Color(0, 0, 0, 0.82f);
		_sheet.interactable = true;
		_sheet.blocksRaycasts = true;

		for (float t = 0.0f; t < SheetFadeSeconds; t += Time.unscaledDeltaTime)
		{
			_sheet.alpha = Ease.SmootherStep(t / SheetFadeSeconds);
			yield return null;
		}

		_sheet.alpha = 1.0f;
		_acceptsInput = true;
	}

	void Update()
	{
		if (_acceptsInput == false)
			return;

		if (AnyKey.EscapeDown)
		{
			_acceptsInput = false;
			Quit();
			return;
		}

		if (AnyKey.Down)
		{
			_acceptsInput = false;
			Retry();
		}
	}

	void Restore()
	{
		AudioListener.volume = _listenerVolume;
		Time.timeScale = 1;

		if (_camera != null)
			_camera.ResetEffects();
	}

	void OnDestroy()
	{
		Restore();
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
