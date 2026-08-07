using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameOver : UI_Popup
{
	const string ArtDir = "Art/UI/Game_Over_Screen/";


	public const string ScreamFallbackClip = "moster growl (4)";

	const float SilenceSeconds = 0.10f;
	const float DefaultFaceSeconds = 0.34f;
	const float ScareFps = 24.0f;
	const float RushStartScale = 0.85f;
	const float RushEndScale = 2.6f;

	// 입이 화면 어디쯤 있는지. 확대는 이 점을 중심으로 일어난다.
	const float MouthPivotY = 0.36f;

	// 증명사진처럼 반듯한 정면이면 안 무섭다. 비스듬히 들어와 각도를 틀며 덮친다.
	const float TiltStartDegrees = -9.0f;
	const float TiltEndDegrees = 4.0f;
	const float EntryOffsetX = -0.09f;
	const float EntryOffsetY = 0.06f;

	const float SettleSeconds = 0.28f;
	const float SheetFadeSeconds = 0.22f;

	const int FlickerCount = 3;
	const float FlickerOnSeconds = 0.03f;
	const float FlickerOffSeconds = 0.03f;

	const float HitstopScale = 0.02f;
	const float JitterPixels = 0.014f;

	Image _face;
	Sprite[] _scare;
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

	void BuildFace()
	{
		RectTransform rect = Stretch("Jumpscare");

		// 확대 기준을 입에 둔다. 화면 정중앙을 기준으로 키우면 얼굴 아래쪽에 있는
		// 입이 밑으로 밀려나 잘린다 — 정작 봐야 할 게 화면 밖으로 나간다.
		rect.pivot = new Vector2(0.5f, MouthPivotY);

		_face = rect.gameObject.AddComponent<Image>();
		_face.preserveAspect = false;
		_face.raycastTarget = false;
		_face.color = Color.white;

		_scare = LoadScareFrames();
		_face.sprite = _scare != null && _scare.Length > 0
			? _scare[0]
			: Resources.Load<Sprite>(ArtDir + (_spec != null ? _spec.FaceArt : "jumpscare_sangju"));

		_face.enabled = false;
	}

	Sprite[] LoadScareFrames()
	{
		string sheet = _spec != null ? _spec.ScareSheet : null;
		if (string.IsNullOrEmpty(sheet))
			return null;

		Sprite[] loaded = Resources.LoadAll<Sprite>(ArtDir + sheet);
		if (loaded == null || loaded.Length == 0)
			return null;

		System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
		return loaded;
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
			_camera.Shake(0.34f, SilenceSeconds + FaceSeconds);

		yield return new WaitForSecondsRealtime(SilenceSeconds);
	}

	static float FlickerSpan
	{
		get { return FlickerCount * (FlickerOnSeconds + FlickerOffSeconds); }
	}

	IEnumerator Flicker()
	{
		Time.timeScale = 0.0f;
		_dim.color = Color.black;

		if (_face.sprite == null)
		{
			_face.enabled = false;
			yield return new WaitForSecondsRealtime(FlickerSpan);
			yield break;
		}

		for (int i = 0; i < FlickerCount; i++)
		{
			_face.enabled = false;
			yield return new WaitForSecondsRealtime(FlickerOffSeconds);

			_face.enabled = true;
			yield return new WaitForSecondsRealtime(FlickerOnSeconds);
		}

		_face.enabled = false;
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

		_dim.color = Color.black;
		_face.enabled = true;
		_flash.color = new Color(0.55f, 0.02f, 0.02f, 0.5f);

		RectTransform faceRect = _face.rectTransform;
		RectTransform root = GetComponent<RectTransform>();
		float amplitude = root.rect.height * JitterPixels;
		int count = _scare != null ? _scare.Length : 1;
		float step = 1.0f / ScareFps;
		float span = Mathf.Max(FaceSeconds, count * step);
		float elapsed = 0.0f;

		while (elapsed < span)
		{
			float k = Mathf.Clamp01(elapsed / span);

			if (_scare != null && _scare.Length > 0)
			{
				int frame = Mathf.Min(count - 1, Mathf.FloorToInt(elapsed / step));
				_face.sprite = _scare[frame];
			}

			// 제자리에서 여닫는 게 아니라 달려든다 — 뒤로 갈수록 가속해서 커진다.
			float rush = Mathf.Lerp(RushStartScale, RushEndScale, k * k);
			faceRect.localScale = new Vector3(rush, rush, 1.0f);

			// 비스듬히 들어와 정면으로 틀며 덮친다.
			faceRect.localRotation = Quaternion.Euler(
				0.0f, 0.0f, Mathf.Lerp(TiltStartDegrees, TiltEndDegrees, Ease.OutQuint(k)));

			// 옆에서 파고들다 입이 화면 한가운데로 온다.
			Vector2 entry = new Vector2(
				root.rect.width * EntryOffsetX, root.rect.height * EntryOffsetY);
			Vector2 drift = Vector2.Lerp(entry, Vector2.zero, Ease.OutQuint(k));

			// 다가올수록 손이 떨리듯 흔들림이 커진다.
			float seed = Time.unscaledTime * 90.0f;
			float shake = amplitude * (0.4f + 1.6f * k);
			faceRect.anchoredPosition = drift + new Vector2(
				(Mathf.PerlinNoise(seed, 0.0f) * 2.0f - 1.0f) * shake,
				(Mathf.PerlinNoise(0.0f, seed) * 2.0f - 1.0f) * shake);

			_flash.color = new Color(
				0.55f, 0.02f, 0.02f, 0.5f * (1.0f - Ease.OutQuint(k)));

			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}

		_dim.color = Color.black;
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
		_dim.color = Color.black;
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
