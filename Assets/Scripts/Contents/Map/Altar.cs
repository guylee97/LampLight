using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Collider2D))]
public class Altar : MonoBehaviour, IInteractable
{
	public const string StepClip = "ritual_step";
	public const string StepFallbackClip = "exit_unlock";
	public const string SealClip = "ritual_seal";

	static int s_completedSteps;

	[SerializeField]
	float _channelSeconds = 8.0f;

	[SerializeField]
	float _glowRadius = 3.2f;

	[SerializeField]
	Color _glowColor = new Color(1.0f, 0.62f, 0.22f, 1.0f);

	[SerializeField]
	PingScheduler _ping;

	[SerializeField]
	SpriteRenderer _renderer;

	StageProgress _progress;
	PlayerInteractor _interactor;
	Lamp _lamp;
	Light2D _glow;
	int _placed;
	bool _finished;
	bool _channeling;

	public Action<int, int> OnStepPlaced;
	public Action OnSealed;

	public static int CompletedSteps { get { return s_completedSteps; } }

	public static void ResetProgress()
	{
		s_completedSteps = 0;
	}

	public int Placed { get { return _placed; } }
	public int Required { get { return _progress == null ? 0 : _progress.Required; } }
	public bool IsSealed { get { return _finished; } }
	public bool IsChanneling { get { return _channeling; } }

	public int Carried
	{
		get
		{
			if (_progress == null)
				return 0;

			return Mathf.Max(0, _progress.Collected - _placed);
		}
	}

	public bool CanInteract { get { return _finished == false && Carried > 0; } }

	public float HoldSeconds { get { return _channelSeconds; } }

	public Vector3 Position { get { return transform.position; } }

	public string Prompt
	{
		get
		{
			if (_finished)
				return "봉인되었다";

			if (Carried > 0)
				return $"[E] 유물을 올린다  {_placed} / {Required}";

			int missing = Mathf.Max(0, Required - _progress.Collected);
			return $"유물 {missing}개가 더 필요하다";
		}
	}

	public void SetChannelSeconds(float seconds)
	{
		if (seconds > 0.0f)
			_channelSeconds = seconds;
	}

	public void Init(StageProgress progress)
	{
		_progress = progress;
		_placed = 0;
		_finished = false;
		_channeling = false;
		ResetProgress();
		ApplyPing();
	}

	void Awake()
	{
		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		if (_ping == null)
			_ping = GetComponent<PingScheduler>();

		EnsureGlow();
	}

	void EnsureGlow()
	{
		if (_glow != null)
			return;

		GameObject go = new GameObject("AltarGlow");
		go.transform.SetParent(transform, false);

		_glow = go.AddComponent<Light2D>();
		_glow.lightType = Light2D.LightType.Point;
		_glow.color = _glowColor;
		_glow.pointLightInnerRadius = 0.0f;
		_glow.pointLightOuterRadius = _glowRadius;
		_glow.pointLightOuterAngle = 360.0f;
		_glow.pointLightInnerAngle = 360.0f;
		_glow.shadowsEnabled = false;
		_glow.intensity = 0.0f;
	}

	void ApplyPing()
	{
		if (_ping != null)
			_ping.Active = _finished == false;
	}

	void Update()
	{
		bool channeling = ResolveChanneling();

		if (channeling != _channeling)
		{
			_channeling = channeling;
			ApplyChannelLighting();
		}

		UpdateGlow();
	}

	bool ResolveChanneling()
	{
		if (_finished)
			return false;

		if (_interactor == null)
		{
			GameObject player = Managers.Game.GetPlayer();
			if (player != null)
				_interactor = player.GetComponent<PlayerInteractor>();
		}

		if (_interactor == null)
			return false;

		return ReferenceEquals(_interactor.Current, this) && _interactor.HoldProgress > 0.0f;
	}

	void ApplyChannelLighting()
	{
		if (ResolveLamp() == false)
			return;

		_lamp.SnuffTo(_channeling ? 0.0f : 1.0f);
	}

	bool ResolveLamp()
	{
		if (_lamp != null)
			return true;

		GameObject player = Managers.Game.GetPlayer();
		PlayerController controller = player != null
			? player.GetComponent<PlayerController>()
			: FindFirstObjectByType<PlayerController>();

		_lamp = controller != null ? controller.Lamp : null;
		return _lamp != null;
	}

	void UpdateGlow()
	{
		if (_glow == null)
			return;

		float baseIntensity = 0.25f + 0.55f * StepRatio;

		if (_channeling)
		{
			float progress = _interactor == null ? 0.0f : _interactor.HoldProgress;
			float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 9.0f);
			float eased = Ease.InOutCubic(progress);
			_glow.intensity = (baseIntensity + 1.35f * eased) * pulse;
			_glow.pointLightOuterRadius = _glowRadius * (1.0f + 0.6f * eased);
			return;
		}

		_glow.intensity = baseIntensity * (0.85f + 0.15f * Mathf.Sin(Time.time * 2.0f));
		_glow.pointLightOuterRadius = _glowRadius;
	}

	float StepRatio
	{
		get
		{
			int required = Required;
			return required <= 0 ? 0.0f : Mathf.Clamp01((float)_placed / required);
		}
	}

	public void Interact(PlayerController player)
	{
		if (CanInteract == false)
			return;

		_placed++;
		s_completedSteps = _placed;

		_channeling = false;

		if (ResolveLamp())
			_lamp.SnuffTo(1.0f);

		Managers.Sound.PlayAtPointOptional(
			StepClip,
			StepFallbackClip,
			transform.position,
			Define.Sound.Guide);

		if (OnStepPlaced != null)
			OnStepPlaced.Invoke(_placed, Required);

		if (_placed < Required)
			return;

		Seal(player);
	}

	void Seal(PlayerController player)
	{
		_finished = true;
		ApplyPing();

		Managers.Sound.PlayAtPointOptional(
			SealClip,
			StepFallbackClip,
			transform.position,
			Define.Sound.Guide);

		HorrorMix.ResetState();

		if (OnSealed != null)
			OnSealed.Invoke();

		float lampRemaining = 0.0f;

		if (player != null && player.Lamp != null)
			lampRemaining = player.Lamp.RemainingDuration;

		int collected = _progress != null ? _progress.Collected : 0;
		float weighted = _progress != null ? _progress.WeightedValue : 0.0f;

		Managers.Game.ReportEscaped(collected, weighted, lampRemaining);
	}

	public void UseCatalogSprite()
	{
		Sprite sprite = Resources.Load<Sprite>("Art/Objects/large/obj_large_altar_low");

		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		if (_renderer != null && sprite != null)
			_renderer.sprite = sprite;
	}
}
