using System.Collections.Generic;
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
			_progress.OnArtifactCollected += OnArtifactCollected;

		CreateSoundRingPool();
		Managers.Sound.OnSpatialSoundPlayed += OnSpatialSoundPlayed;
	}

	void OnDestroy()
	{
		if (_progress != null)
			_progress.OnArtifactCollected -= OnArtifactCollected;

		if (Managers.TryGetSound(out SoundManager sound))
			sound.OnSpatialSoundPlayed -= OnSpatialSoundPlayed;
	}

	void OnArtifactCollected(int collected, int required)
	{
		RefreshArtifacts();
	}

	void RefreshArtifacts()
	{
		if (_ready == false || _progress == null)
			return;

		Text text = GetText((int)Texts.ArtifactText);
		if (text != null)
			text.text = $"유물  {_progress.Collected} / {_progress.Required}";
	}

	void Update()
	{
		if (_ready == false)
			return;

		UpdateSoundRings();
		UpdateFill(GetImage((int)Images.StaminaFill), _status == null ? 0 : _status.StaminaRatio);
		UpdateFill(GetImage((int)Images.FuelFill), _lamp == null ? 0 : _lamp.RemainingRatio);

		Text fuelText = GetText((int)Texts.FuelText);
		if (fuelText != null && _lamp != null)
			fuelText.text = _lamp.IsOn ? $"등불  {Mathf.CeilToInt(_lamp.RemainingDuration)}s" : "등불  꺼짐";

		Text prompt = GetText((int)Texts.PromptText);
		if (prompt != null)
		{
			IInteractable target = _interactor == null ? null : _interactor.Current;
			prompt.text = target == null ? string.Empty : target.Prompt;
		}
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
