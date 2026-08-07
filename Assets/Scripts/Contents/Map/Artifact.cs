using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Collider2D))]
public class Artifact : MonoBehaviour, IInteractable
{
	[SerializeField, Min(0.1f)]
	float _glowRadiusTiles = 5.0f;

	[SerializeField]
	float _glowIntensity = 0.9f;

	[SerializeField]
	Color _glowColor = new Color(1.0f, 0.83f, 0.45f, 1.0f);

	[SerializeField]
	float _glowPulseSpeed = 2.2f;

	Light2D _glow;
	Transform _listener;

	[SerializeField]
	string _pointName;

	[SerializeField]
	SpriteRenderer _renderer;

	[SerializeField, Range(0, 2)]
	int _concealment;

	[SerializeField]
	float _collectNoiseRadius = 12.0f;

	[SerializeField]
	float _collectNoiseDuration = 1.5f;

	StageProgress _progress;
	bool _collected;

	public Action<Artifact> OnCollected;

	public string PointName { get { return _pointName; } }
	public bool IsCollected { get { return _collected; } }
	public float CollectNoiseRadius { get { return _collectNoiseRadius; } }

	public int Concealment { get { return _concealment; } }
	public float HoldSeconds { get { return ConcealmentRules.HoldSeconds(_concealment); } }
	public float RadiusScale { get { return ConcealmentRules.RadiusScale(_concealment); } }
	public float ScoreWeight { get { return ConcealmentRules.ScoreWeight(_concealment); } }
	public float NoiseRadius { get { return ConcealmentRules.NoiseRadius(_concealment); } }

	public bool CanInteract { get { return _collected == false; } }

	public string Prompt
	{
		get
		{
			if (_concealment == 0)
				return "[E] 공양물 줍기";

			return _concealment == 1 ? "[E] 잔해를 헤친다" : "[E] 석관을 연다";
		}
	}

	public Vector3 Position { get { return transform.position; } }

	public void SetConcealment(int level)
	{
		_concealment = Mathf.Clamp(level, 0, 2);
	}

	void Update()
	{
		if (_collected)
		{
			if (_glow != null)
				_glow.enabled = false;

			return;
		}

		if (ResolveListener() == false)
		{
			if (_glow != null)
				_glow.enabled = false;

			return;
		}

		float distance = Vector2.Distance(transform.position, _listener.position);
		float near = Mathf.Clamp01(1.0f - distance / _glowRadiusTiles);

		if (near <= 0.0f)
		{
			if (_glow != null)
				_glow.enabled = false;

			return;
		}

		EnsureGlow();

		float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * _glowPulseSpeed);
		_glow.enabled = true;
		_glow.intensity = _glowIntensity * near * near * pulse;
		_glow.pointLightOuterRadius = _glowRadiusTiles * 0.5f * (0.6f + 0.4f * near);
	}

	bool ResolveListener()
	{
		if (_listener != null)
			return true;

		PlayerController player = FindFirstObjectByType<PlayerController>();
		if (player == null)
			return false;

		_listener = player.transform;
		return true;
	}

	void EnsureGlow()
	{
		if (_glow != null)
			return;

		GameObject go = new GameObject("ArtifactGlow");
		go.transform.SetParent(transform, false);

		_glow = go.AddComponent<Light2D>();
		_glow.lightType = Light2D.LightType.Point;
		_glow.color = _glowColor;
		_glow.pointLightInnerRadius = 0.0f;
		_glow.pointLightOuterAngle = 360.0f;
		_glow.pointLightInnerAngle = 360.0f;
		_glow.shadowsEnabled = false;
	}

	public void Init(StageProgress progress, string pointName)
	{
		_progress = progress;
		_pointName = pointName;
		_collected = false;
	}

	public void Init(StageProgress progress, string pointName, int concealment)
	{
		Init(progress, pointName);
		SetConcealment(concealment);
	}

	public void Interact(PlayerController player)
	{
		if (TryCollect() == false)
			return;

		if (player != null)
			player.EmitNoise(NoiseRadius, _collectNoiseDuration);
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (_concealment != 0 || _collected)
			return;

		PlayerController player = other.GetComponentInParent<PlayerController>();
		if (player == null)
			return;

		if (TryCollect())
			player.EmitNoise(NoiseRadius, _collectNoiseDuration);
	}

	public bool TryCollect()
	{
		if (_collected)
			return false;

		_collected = true;

		if (_progress != null)
			_progress.ReportCollected(ScoreWeight);

		Managers.Sound.PlayAtPointOptional("artifact_pickup", transform.position, Define.Sound.Self,
			NoiseRadius);

		if (OnCollected != null)
			OnCollected.Invoke(this);

		Managers.Sound.PlayAtPointOptional(
			"유물 획득 소리/👍litupsubway-key-collect-sfx-522219",
			"artifact_pickup",
			transform.position,
			Define.Sound.Guide
		);

		gameObject.SetActive(false);
		return true;
	}

	public void SetSprite(Sprite sprite)
	{
		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		if (_renderer != null && sprite != null)
			_renderer.sprite = sprite;
	}
}
