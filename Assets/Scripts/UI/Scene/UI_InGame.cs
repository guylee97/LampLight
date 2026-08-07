using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : UI_Scene
{
	enum Texts
	{
		ArtifactText,
		PromptText,
		FuelText,
	}

	enum Images
	{
		FuelFill,
		StaminaFill,
	}

	[SerializeField]
	float _fillSpeed = 5.0f;

	StageProgress _progress;
	PlayerController _player;
	PlayerStatus _status;
	PlayerInteractor _interactor;
	Lamp _lamp;
	bool _ready;
	Text _noticeText;
	Coroutine _noticeRoutine;
	GameObject _holdProgressRoot;
	Text _holdProgressText;
	Image _wickFill;
	Image _wickFlame;
	RectTransform _wickRoot;

	public void Setup(StageProgress progress, PlayerController player)
	{
		_progress = progress;
		_player = player;

		if (_player != null)
		{
			_status = _player.Status;
			_lamp = _player.Lamp;
			_interactor = _player.GetComponent<PlayerInteractor>();
		}

		RefreshArtifacts();
	}

	public override void Init()
	{
		base.Init();

		Bind<Text>(typeof(Texts));
		Bind<Image>(typeof(Images));

		_ready = true;

		if (_progress == null)
			Setup(FindFirstObjectByType<StageProgress>(), FindFirstObjectByType<PlayerController>());
		else
			RefreshArtifacts();

		if (_progress != null)
		{
			_progress.OnArtifactCollected += OnArtifactCollected;
			_progress.OnAllArtifactsCollected += OnAllArtifactsCollected;
		}

		HideStatusBars();
		CreateNoticeText();
		CreateHoldProgress();
		CreateWickGauge();
	}

	void OnDestroy()
	{
		if (_progress != null)
		{
			_progress.OnArtifactCollected -= OnArtifactCollected;
			_progress.OnAllArtifactsCollected -= OnAllArtifactsCollected;
		}
	}

	void OnArtifactCollected(int collected, int required)
	{
		RefreshArtifacts();
	}

	void OnAllArtifactsCollected()
	{
		if (_noticeRoutine != null)
			StopCoroutine(_noticeRoutine);
		_noticeRoutine = StartCoroutine(ShowNotice());
	}

	IEnumerator ShowNotice()
	{
		if (_noticeText == null)
			yield break;

		_noticeText.text = string.Empty;
		yield return null;
		_noticeRoutine = null;
	}

	const string WickSprite = "Art/UI/Play_screen_UI/Lantern_Remaining_Wick";
	const string FlameSprite = "Art/UI/Play_screen_UI/firelight";
	const float WickHeight = 206.0f;
	const float WickWidth = 16.0f;

	/// 등불은 이 게임의 유일한 시계다. 숫자 대신 아티스트가 만든 심지가 타들어간다.
	void CreateWickGauge()
	{
		if (_wickRoot != null)
			return;

		Sprite wick = Resources.Load<Sprite>(WickSprite);
		if (wick == null)
		{
			Debug.LogWarning($"UI_InGame: Resources/{WickSprite} 없음");
			return;
		}

		GameObject root = new GameObject("WickGauge", typeof(RectTransform));
		root.transform.SetParent(transform, false);
		_wickRoot = root.GetComponent<RectTransform>();
		_wickRoot.anchorMin = new Vector2(0.0f, 0.0f);
		_wickRoot.anchorMax = new Vector2(0.0f, 0.0f);
		_wickRoot.pivot = new Vector2(0.0f, 0.0f);
		_wickRoot.anchoredPosition = new Vector2(34.0f, 34.0f);
		_wickRoot.sizeDelta = new Vector2(WickWidth, WickHeight);

		GameObject fill = new GameObject("Wick", typeof(RectTransform), typeof(Image));
		fill.transform.SetParent(root.transform, false);
		RectTransform fillRect = fill.GetComponent<RectTransform>();
		fillRect.anchorMin = Vector2.zero;
		fillRect.anchorMax = Vector2.one;
		fillRect.offsetMin = Vector2.zero;
		fillRect.offsetMax = Vector2.zero;

		_wickFill = fill.GetComponent<Image>();
		_wickFill.sprite = wick;
		_wickFill.preserveAspect = false;
		_wickFill.raycastTarget = false;
		_wickFill.type = Image.Type.Filled;
		_wickFill.fillMethod = Image.FillMethod.Vertical;
		_wickFill.fillOrigin = (int)Image.OriginVertical.Bottom;
		_wickFill.fillAmount = 1.0f;

		Sprite flame = Resources.Load<Sprite>(FlameSprite);
		if (flame == null)
			return;

		GameObject head = new GameObject("Flame", typeof(RectTransform), typeof(Image));
		head.transform.SetParent(root.transform, false);
		RectTransform headRect = head.GetComponent<RectTransform>();
		headRect.anchorMin = new Vector2(0.5f, 0.0f);
		headRect.anchorMax = new Vector2(0.5f, 0.0f);
		headRect.pivot = new Vector2(0.5f, 0.5f);
		headRect.sizeDelta = new Vector2(26.0f, 26.0f);

		_wickFlame = head.GetComponent<Image>();
		_wickFlame.sprite = flame;
		_wickFlame.raycastTarget = false;
	}

	void UpdateWickGauge()
	{
		if (_wickFill == null)
			return;

		float ratio = _lamp == null ? 0.0f : Mathf.Clamp01(_lamp.RemainingRatio);
		_wickFill.fillAmount = ratio;

		if (_wickFlame == null)
			return;

		// 불꽃은 남은 심지 끝에 앉아 함께 내려온다. 다 타면 꺼진다.
		_wickFlame.enabled = ratio > 0.0f;
		_wickFlame.rectTransform.anchoredPosition = new Vector2(0.0f, WickHeight * ratio);

		float flicker = 0.85f + Mathf.PingPong(Time.unscaledTime * 1.7f, 0.3f);
		_wickFlame.rectTransform.localScale = new Vector3(flicker, flicker, 1.0f);
	}

	void RefreshArtifacts()
	{
		if (_ready == false)
			return;

		Text text = GetText((int)Texts.ArtifactText);
		if (text != null && text.enabled)
			text.enabled = false;
	}

	void Update()
	{
		if (_ready == false)
			return;

		UpdateWickGauge();

		Text fuelText = GetText((int)Texts.FuelText);
		if (fuelText != null && fuelText.enabled)
			fuelText.enabled = false;

		Text prompt = GetText((int)Texts.PromptText);
		if (prompt != null)
		{
			IInteractable target = _interactor == null ? null : _interactor.Current;
			prompt.text = target == null ? string.Empty : target.Prompt;
		}

		UpdateHoldProgress();
	}

	void CreateHoldProgress()
	{
		if (_holdProgressRoot != null)
			return;

		_holdProgressRoot = new GameObject("InteractionProgress", typeof(RectTransform));
		_holdProgressRoot.transform.SetParent(transform, false);

		RectTransform rootRect = _holdProgressRoot.GetComponent<RectTransform>();
		rootRect.anchorMin = new Vector2(0.5f, 0.0f);
		rootRect.anchorMax = new Vector2(0.5f, 0.0f);
		rootRect.pivot = new Vector2(0.5f, 0.0f);
		rootRect.anchoredPosition = new Vector2(0.0f, 88.0f);
		rootRect.sizeDelta = new Vector2(480.0f, 64.0f);

		GameObject textObject = new GameObject("RemainingText", typeof(RectTransform), typeof(Text));
		textObject.transform.SetParent(_holdProgressRoot.transform, false);
		RectTransform textRect = textObject.GetComponent<RectTransform>();
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = Vector2.zero;
		textRect.offsetMax = Vector2.zero;

		_holdProgressText = textObject.GetComponent<Text>();
		_holdProgressText.font = KoreanFont.Font;
		_holdProgressText.fontSize = 30;
		_holdProgressText.alignment = TextAnchor.MiddleCenter;
		_holdProgressText.color = new Color(1.0f, 0.82f, 0.42f, 1.0f);
		_holdProgressText.raycastTarget = false;

		_holdProgressRoot.SetActive(false);
	}

	void UpdateHoldProgress()
	{
		if (_holdProgressRoot == null || _holdProgressText == null)
			return;

		bool visible = _interactor != null
			&& _interactor.Current != null
			&& _interactor.Current.HoldSeconds > 0.0f
			&& _interactor.HoldProgress > 0.0f;

		_holdProgressRoot.SetActive(visible);
		if (visible)
			_holdProgressText.text = $"뒤지는 중  {_interactor.HoldRemainingSeconds:0.0}초";
	}

	void HideStatusBars()
	{
		foreach (Images value in System.Enum.GetValues(typeof(Images)))
		{
			Image image = GetImage((int)value);
			if (image != null)
				image.transform.parent.gameObject.SetActive(false);
		}
	}

	void CreateNoticeText()
	{
		if (_noticeText != null)
			return;

		GameObject go = new GameObject("ExitNoticeText", typeof(RectTransform), typeof(Text));
		go.transform.SetParent(transform, false);
		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 0.08f);
		rect.anchorMax = new Vector2(0.5f, 0.08f);
		rect.sizeDelta = new Vector2(600, 60);
		_noticeText = go.GetComponent<Text>();
		_noticeText.font = KoreanFont.Font;
		_noticeText.fontSize = 30;
		_noticeText.alignment = TextAnchor.MiddleCenter;
		_noticeText.color = Color.white;
	}

	void UpdateFill(Image image, float target)
	{
		if (image == null)
			return;

		image.fillAmount = Mathf.MoveTowards(image.fillAmount, target, _fillSpeed * Time.unscaledDeltaTime);
	}
}
