using System.Collections.Generic;
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

	[SerializeField]
	float _soundRingDuration = 0.6f;

	[SerializeField]
	float _soundRingRadius = 190.0f;

	[SerializeField]
	float _soundRingThickness = 7.0f;

	[SerializeField]
	float _soundRingArcAngle = 42.0f;

	[SerializeField]
	Sprite _soundArcSprite;

	StageProgress _progress;
	PlayerController _player;
	PlayerStatus _status;
	PlayerInteractor _interactor;
	Lamp _lamp;
	RectTransform _soundRingRoot;
	readonly List<SoundRing> _soundRings = new List<SoundRing>();
	bool _ready;
	float _remainingSeconds;
	Text _noticeText;
	Coroutine _noticeRoutine;
	GameObject _holdProgressRoot;
	Text _holdProgressText;
	Altar _altar;
	Image _arrow;
	Sprite _arrowSprite;

	public void Setup(StageProgress progress, PlayerController player, float deadlineSeconds = 0.0f)
	{
		_progress = progress;
		_player = player;

		if (_player != null)
		{
			_status = _player.Status;
			_lamp = _player.Lamp;
			_interactor = _player.GetComponent<PlayerInteractor>();
		}

		_remainingSeconds = deadlineSeconds;
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
		CreateObjectiveArrow();

		CreateSoundRingPool();
		Managers.Sound.OnSpatialSoundPlayed += OnSpatialSoundPlayed;
	}

	void OnDestroy()
	{
		if (_progress != null)
		{
			_progress.OnArtifactCollected -= OnArtifactCollected;
			_progress.OnAllArtifactsCollected -= OnAllArtifactsCollected;
		}

		if (Managers.TryGetSound(out SoundManager sound))
			sound.OnSpatialSoundPlayed -= OnSpatialSoundPlayed;
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

		_noticeText.text = "제단으로 가라";
		yield return new WaitForSecondsRealtime(2.5f);
		_noticeText.text = string.Empty;
		_noticeRoutine = null;
	}

	public void SetRemainingTime(float seconds)
	{
		_remainingSeconds = seconds;
	}

	void RefreshArtifacts()
	{
		if (_ready == false || _progress == null)
			return;

		Text text = GetText((int)Texts.ArtifactText);
		if (text == null)
			return;

		RectTransform rect = text.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 1.0f);
		rect.anchorMax = new Vector2(0.5f, 1.0f);
		rect.pivot = new Vector2(0.5f, 1.0f);
		rect.anchoredPosition = new Vector2(0.0f, -84.0f);
		rect.sizeDelta = new Vector2(720.0f, 48.0f);
		text.alignment = TextAnchor.MiddleCenter;
		text.text = ObjectiveLine();
	}

	string ObjectiveLine()
	{
		if (_progress == null)
			return string.Empty;

		ResolveAltar();

		if (_altar != null && _altar.IsSealed)
			return "봉인 완료";

		if (_altar != null && _altar.Carried > 0)
			return $"제단에서 의식을 치러라   ·   봉인 {_altar.Placed} / {_altar.Required}";

		int missing = Mathf.Max(0, _progress.Required - _progress.Collected);

		if (missing > 0)
			return $"유물을 찾아라   ·   {_progress.Collected} / {_progress.Required}";

		return "제단으로 가라";
	}

	void ResolveAltar()
	{
		if (_altar == null)
			_altar = FindFirstObjectByType<Altar>();
	}

	void Update()
	{
		if (_ready == false)
			return;

		UpdateSoundRings();
		RefreshArtifacts();
		UpdateObjectiveArrow();

		Text fuelText = GetText((int)Texts.FuelText);
		if (fuelText != null)
		{
			RectTransform fuelRect = fuelText.rectTransform;
			fuelRect.anchorMin = new Vector2(0.5f, 1.0f);
			fuelRect.anchorMax = new Vector2(0.5f, 1.0f);
			fuelRect.pivot = new Vector2(0.5f, 1.0f);
			fuelRect.anchoredPosition = new Vector2(0.0f, -28.0f);
			fuelRect.sizeDelta = new Vector2(420.0f, 55.0f);
			fuelText.alignment = TextAnchor.MiddleCenter;

			int seconds = Mathf.CeilToInt(_remainingSeconds);
			fuelText.text = $"남은 시간  {seconds / 60:00}:{seconds % 60:00}";
		}

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

	void CreateObjectiveArrow()
	{
		if (_arrow != null)
			return;

		GameObject go = new GameObject("ObjectiveArrow", typeof(RectTransform), typeof(Image));
		go.transform.SetParent(transform, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.sizeDelta = new Vector2(44.0f, 44.0f);

		_arrow = go.GetComponent<Image>();
		_arrow.sprite = ArrowSprite();
		_arrow.raycastTarget = false;
		_arrow.color = new Color(1.0f, 0.72f, 0.32f, 0.9f);
		_arrow.enabled = false;
	}

	Sprite ArrowSprite()
	{
		if (_arrowSprite != null)
			return _arrowSprite;

		const int size = 32;
		Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float half = (size - 1 - y) * 0.5f;
				float dx = Mathf.Abs(x - (size - 1) * 0.5f);
				bool inside = y >= 4 && dx <= half;
				texture.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
			}
		}

		texture.Apply();
		_arrowSprite = Sprite.Create(
			texture,
			new Rect(0, 0, size, size),
			new Vector2(0.5f, 0.5f),
			32.0f);

		return _arrowSprite;
	}

	void UpdateObjectiveArrow()
	{
		if (_arrow == null)
			return;

		Vector3 target;
		if (TryResolveObjectiveTarget(out target) == false)
		{
			_arrow.enabled = false;
			return;
		}

		Camera camera = Camera.main;
		if (camera == null || _player == null)
		{
			_arrow.enabled = false;
			return;
		}

		Vector3 viewport = camera.WorldToViewportPoint(target);
		bool onScreen = viewport.z > 0.0f
			&& viewport.x > 0.08f && viewport.x < 0.92f
			&& viewport.y > 0.08f && viewport.y < 0.92f;

		if (onScreen)
		{
			_arrow.enabled = false;
			return;
		}

		Vector2 direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);

		if (viewport.z < 0.0f)
			direction = -direction;

		if (direction.sqrMagnitude <= 0.000001f)
		{
			_arrow.enabled = false;
			return;
		}

		direction.Normalize();

		RectTransform root = GetComponent<RectTransform>();
		float radius = Mathf.Min(root.rect.width, root.rect.height) * 0.34f;

		_arrow.enabled = true;
		_arrow.rectTransform.anchoredPosition = direction * radius;

		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90.0f;
		_arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
	}

	bool TryResolveObjectiveTarget(out Vector3 target)
	{
		target = Vector3.zero;

		if (_progress == null)
			return false;

		ResolveAltar();

		if (_altar != null && _altar.IsSealed)
			return false;

		if (_altar != null && (_altar.Carried > 0 || _progress.IsComplete))
		{
			target = _altar.Position;
			return true;
		}

		Artifact nearest = null;
		float bestSqr = float.MaxValue;

		foreach (Artifact artifact in FindObjectsByType<Artifact>(FindObjectsSortMode.None))
		{
			if (artifact.IsCollected)
				continue;

			float sqr = (artifact.transform.position - _player.transform.position).sqrMagnitude;
			if (sqr >= bestSqr)
				continue;

			nearest = artifact;
			bestSqr = sqr;
		}

		if (nearest == null)
			return false;

		target = nearest.transform.position;
		return true;
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

	void CreateSoundRingPool()
	{
		if (_soundRingRoot != null)
			return;

		GameObject root = new GameObject("SoundRingRoot", typeof(RectTransform));
		root.transform.SetParent(transform, false);
		_soundRingRoot = root.GetComponent<RectTransform>();
		_soundRingRoot.anchorMin = Vector2.zero;
		_soundRingRoot.anchorMax = Vector2.one;
		_soundRingRoot.offsetMin = Vector2.zero;
		_soundRingRoot.offsetMax = Vector2.zero;
		_soundRingRoot.SetAsLastSibling();

		for (int i = 0; i < 8; i++)
		{
			GameObject go = new GameObject($"SoundRing_{i}", typeof(RectTransform), typeof(Image));
			go.transform.SetParent(_soundRingRoot, false);

			Image image = go.GetComponent<Image>();
			image.sprite = _soundArcSprite;
			image.raycastTarget = false;
			image.enabled = false;

			RectTransform rect = go.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);

			_soundRings.Add(new SoundRing(rect, image));
		}
	}

	void OnSpatialSoundPlayed(
		Vector3 worldPosition,
		Define.Sound bus,
		float volume,
		float uncertainty)
	{
		if (bus != Define.Sound.Guide &&
			bus != Define.Sound.Threat &&
			bus != Define.Sound.Self)
			return;

		Camera camera = Camera.main;
		if (camera == null || _soundRingRoot == null)
			return;

		Vector3 sourceViewport = camera.WorldToViewportPoint(worldPosition);
		Vector3 playerViewport = _player == null
			? new Vector3(0.5f, 0.5f)
			: camera.WorldToViewportPoint(_player.transform.position);
		Vector2 direction = new Vector2(
			sourceViewport.x - playerViewport.x,
			sourceViewport.y - playerViewport.y
		);

		if (direction.sqrMagnitude <= 0.0001f)
			direction = Vector2.down;

		direction.Normalize();
		SoundRing ring = GetAvailableSoundRing();
		Vector2 playerScreenPoint = new Vector2(
			playerViewport.x * Screen.width,
			playerViewport.y * Screen.height
		);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_soundRingRoot,
			playerScreenPoint,
			null,
			out Vector2 playerLocalPoint
		);
		PlaceSoundRing(ring, playerLocalPoint, direction, uncertainty);

		Color color = GetSoundRingColor(bus);
		float volumeAlpha = Mathf.Lerp(0.65f, 1.0f, volume);
		color.a = Mathf.Lerp(0.9f, 0.35f, uncertainty) * volumeAlpha;
		ring.BaseColor = color;
		ring.Image.color = color;
		ring.Image.enabled = true;
		ring.ExpiresAt = Time.unscaledTime + _soundRingDuration;
	}

	void PlaceSoundRing(
		SoundRing ring,
		Vector2 center,
		Vector2 direction,
		float uncertainty)
	{
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		float arcAngle = Mathf.Lerp(28.0f, 105.0f, uncertainty);

		if (_soundArcSprite != null)
		{
			ring.Rect.anchoredPosition = center;
			float diameter = _soundRingRadius * 2.0f;
			float widthScale = arcAngle / Mathf.Max(1.0f, _soundRingArcAngle);
			ring.Rect.sizeDelta = new Vector2(diameter * widthScale, diameter);
			ring.Rect.localRotation = Quaternion.Euler(0, 0, angle - 90.0f);
			return;
		}

		float fallbackWidth = 2.0f * Mathf.PI * _soundRingRadius * arcAngle / 360.0f;
		ring.Rect.anchoredPosition = center + direction * _soundRingRadius;
		ring.Rect.sizeDelta = new Vector2(fallbackWidth, _soundRingThickness);
		ring.Rect.localRotation = Quaternion.Euler(0, 0, angle + 90.0f);
	}

	void UpdateSoundRings()
	{
		for (int i = 0; i < _soundRings.Count; i++)
		{
			SoundRing ring = _soundRings[i];
			if (ring.Image.enabled == false)
				continue;

			float remaining = ring.ExpiresAt - Time.unscaledTime;
			if (remaining <= 0)
			{
				ring.Image.enabled = false;
				continue;
			}

			Color color = ring.BaseColor;
			color.a *= Mathf.Clamp01(remaining / _soundRingDuration);
			ring.Image.color = color;
		}
	}

	SoundRing GetAvailableSoundRing()
	{
		SoundRing oldest = _soundRings[0];

		for (int i = 0; i < _soundRings.Count; i++)
		{
			SoundRing ring = _soundRings[i];
			if (ring.Image.enabled == false)
				return ring;

			if (ring.ExpiresAt < oldest.ExpiresAt)
				oldest = ring;
		}

		return oldest;
	}

	Color GetSoundRingColor(Define.Sound bus)
	{
		switch (bus)
		{
			case Define.Sound.Guide:
				return new Color(1.0f, 0.55f, 0.12f, 1.0f);
			case Define.Sound.Threat:
				return new Color(0.9f, 0.12f, 0.1f, 1.0f);
			default:
				return new Color(0.65f, 0.68f, 0.72f, 1.0f);
		}
	}

	void UpdateFill(Image image, float target)
	{
		if (image == null)
			return;

		image.fillAmount = Mathf.MoveTowards(image.fillAmount, target, _fillSpeed * Time.unscaledDeltaTime);
	}

	sealed class SoundRing
	{
		public readonly RectTransform Rect;
		public readonly Image Image;
		public Color BaseColor;
		public float ExpiresAt;

		public SoundRing(RectTransform rect, Image image)
		{
			Rect = rect;
			Image = image;
		}
	}
}
