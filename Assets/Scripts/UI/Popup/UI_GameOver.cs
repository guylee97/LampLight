using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameOver : UI_Popup
{
	const string ArtDir = "Art/UI/Game_Over_Screen/";


	public const string ScreamFallbackClip = "moster growl (4)";

	const float SilenceSeconds = 0.10f;
	const float LungeSeconds = 0.30f;
	const float DefaultFaceSeconds = 0.34f;

	const float SettleSeconds = 0.28f;
	const float SheetFadeSeconds = 0.22f;

	const int FlickerCount = 3;
	const float FlickerOnSeconds = 0.03f;
	const float FlickerOffSeconds = 0.03f;
	const float FlickerDim = 0.88f;

	const float HitstopScale = 0.02f;
	const float JitterPixels = 0.014f;

	Image _face;
	Image _lunge;
	Image _flash;
	Image _dim;
	CanvasGroup _sheet;
	CameraController _camera;
	PressAnyKeyPrompt _prompt;
	YokaiSpec _spec;
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

		_spec = ResolveSpec();

		_dim = Stretch("Dim").gameObject.AddComponent<Image>();
		_dim.color = new Color(0, 0, 0, 0);
		_dim.raycastTarget = false;

		BuildLunge();
		BuildFace();

		_flash = Stretch("Flash").gameObject.AddComponent<Image>();
		_flash.color = new Color(0.55f, 0.02f, 0.02f, 0.0f);
		_flash.raycastTarget = false;

		BuildSheet();

		_camera = FindFirstObjectByType<CameraController>();
		_listenerVolume = AudioListener.volume;

		StartCoroutine(Play());
	}

	YokaiSpec ResolveSpec()
	{
		Transform catcher = Managers.Game.Catcher;
		MaskYokai yokai = catcher == null ? null : catcher.GetComponentInParent<MaskYokai>();

		return yokai != null ? yokai.Spec : YokaiTable.ForLevel(Managers.Game.CurrentLevel);
	}

	float FaceSeconds
	{
		get { return _spec != null ? _spec.FaceHoldSeconds : DefaultFaceSeconds; }
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

	void BuildLunge()
	{
		Transform catcher = Managers.Game.Catcher;
		if (catcher == null)
			return;

		SpriteRenderer source = catcher.GetComponentInChildren<SpriteRenderer>();
		if (source == null || source.sprite == null)
			return;

		GameObject go = new GameObject("Lunge", typeof(RectTransform), typeof(Image));
		go.transform.SetParent(transform, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);

		Sprite sprite = source.sprite;
		float aspect = sprite.rect.width / Mathf.Max(1.0f, sprite.rect.height);
		rect.sizeDelta = new Vector2(1080.0f * aspect, 1080.0f);

		_lunge = go.GetComponent<Image>();
		_lunge.sprite = sprite;
		_lunge.preserveAspect = true;
		_lunge.raycastTarget = false;
		_lunge.color = _spec != null ? _spec.Tint : Color.white;
		_lunge.enabled = false;
	}

	IEnumerator Lunge()
	{
		if (_lunge == null)
			yield break;

		Camera camera = Camera.main;
		Transform catcher = Managers.Game.Catcher;
		RectTransform root = GetComponent<RectTransform>();
		RectTransform rect = _lunge.rectTransform;

		Vector2 from = Vector2.zero;

		if (camera != null && catcher != null)
		{
			Vector3 viewport = camera.WorldToViewportPoint(catcher.position);
			from = new Vector2(
				(viewport.x - 0.5f) * root.rect.width,
				(viewport.y - 0.5f) * root.rect.height);
		}

		_lunge.enabled = true;

		for (float t = 0.0f; t < LungeSeconds; t += Time.unscaledDeltaTime)
		{
			float k = Ease.OutQuint(t / LungeSeconds);
			float scale = Mathf.Lerp(0.55f, _spec != null ? _spec.LungeScale : 3.1f, k);

			rect.localScale = new Vector3(scale, scale, 1.0f);
			rect.anchoredPosition = Vector2.Lerp(from, Vector2.zero, k);
			Color tint = _spec != null ? _spec.Tint : Color.white;
			float dim = Mathf.Lerp(1.0f, 0.32f, k);
			_lunge.color = new Color(tint.r * dim, tint.g * dim, tint.b * dim, 1.0f);
			yield return null;
		}

		_lunge.enabled = false;
	}

	void BuildFace()
	{
		RectTransform rect = Stretch("Jumpscare");
		_face = rect.gameObject.AddComponent<Image>();
		_face.sprite = Resources.Load<Sprite>(ArtDir + (_spec != null ? _spec.FaceArt : "jumpscare_face"));
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
		_prompt = PressAnyKeyPrompt.Attach(
			sheet, PressAnyKeyPrompt.PressAnyKeyArt, 0.30f, 520.0f);
		PressAnyKeyPrompt.Attach(
			sheet, PressAnyKeyPrompt.EscTitleArt, 0.215f, 250.0f);
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
		yield return Lunge();
		yield return Face();
		yield return Flicker();
		yield return Settle();
		yield return RevealSheet();
	}

	IEnumerator Hitstop()
	{
		Time.timeScale = HitstopScale;
		AudioListener.volume = 0.0f;

		if (_camera != null)
			_camera.Shake(0.28f, SilenceSeconds + LungeSeconds + FaceSeconds);

		yield return new WaitForSecondsRealtime(SilenceSeconds);
	}

	static float FlickerSpan
	{
		get { return FlickerCount * (FlickerOnSeconds + FlickerOffSeconds); }
	}

	IEnumerator Flicker()
	{
		Time.timeScale = 0.0f;
		_face.enabled = false;

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
		Managers.Sound.PlayOptional(
			_spec != null ? _spec.ScreamClip : "jumpscare_scream",
			ScreamFallbackClip,
			Define.Sound.Threat);

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
		Time.timeScale = 1.0f;
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
