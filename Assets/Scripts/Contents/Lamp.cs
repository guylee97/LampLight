using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class Lamp : MonoBehaviour
{
	[SerializeField]
	float _range = 8.0f;

	[SerializeField]
	float _angle = 55.0f;

	[SerializeField]
	float _maxDuration = 90.0f;

	[SerializeField]
	bool _isOn = true;

	[SerializeField]
	float _intensity = 1.5f;

	[SerializeField, Range(0.0f, 1.0f)]
	float _shadowIntensity = 1.0f;

	[SerializeField]
	float _innerRangeRatio = 0.15f;

	[SerializeField]
	float _orbRadius = 2.9f;

	// 빛은 스프라이트가 실제로 그려지는 한가운데에 붙인다. 피벗이 발밑이든
	// 어디든 렌더 경계에서 직접 구하므로 어긋날 여지가 없다. 바라보는 쪽으로
	// 밀지도 않는다 — 방향마다 빛이 미끄러지면 캐릭터와 따로 노는 것처럼 보인다.
	[SerializeField]
	Vector2 _bodyOffset = Vector2.zero;

	SpriteRenderer _bodyRenderer;

	[SerializeField]
	Color _warmColor = new Color(1.0f, 0.576f, 0.161f, 1.0f);

	[SerializeField, Range(0.0f, 0.5f)]
	float _flickerAmount = 0.12f;

	[SerializeField]
	float _flickerSpeed = 7.0f;

	[SerializeField, Range(0.0f, 0.5f)]
	float _flickerRadiusAmount = 0.05f;

	[SerializeField]
	Light2D _light;


	[SerializeField]
	LayerMask _obstacleMask;

	[SerializeField]
	float _listenRangeRatio = 0.5f;

	[SerializeField]
	float _listenAngle = 45.0f;

	[SerializeField]
	float _listenFuelScale = 0.6f;

	[SerializeField]
	float _listenHearingScale = 1.3f;

	float _remainingDuration;
	float _snuffScale = 1.0f;
	float _fuelScale = 1.0f;
	bool _listening;

	public const string LowFuelClip = "lantern_low";
	public const string BurnedOutClip = "lantern_out";
	public const float LowFuelRatio = 0.25f;

	bool _lowFuelWarned;

	public Action<float> OnFuelChanged;
	public Action OnBurnedOut;
	public Action<bool> OnToggled;

	public bool IsOn { get { return _isOn && _remainingDuration > 0; } }
	public bool IsSwitchedOn { get { return _isOn; } }
	public bool HasFuel { get { return _remainingDuration > 0; } }
	public float Range { get { return _listening ? _orbRadius * _listenRangeRatio : _orbRadius; } }
	public float Angle { get { return _angle; } }
	public float MaxDuration { get { return _maxDuration; } }
	public float RemainingDuration { get { return _remainingDuration; } }
	public float RemainingRatio { get { return _maxDuration <= 0 ? 0 : _remainingDuration / _maxDuration; } }

	void Awake()
	{
		_remainingDuration = _maxDuration;

		if (_light == null)
			_light = GetComponent<Light2D>();

		if (_light == null)
			_light = gameObject.AddComponent<Light2D>();


		ApplyLightSettings();
	}

	void Update()
	{
		StickToBody();
		UpdateDuration();
		ApplyLightSettings();
	}

	void StickToBody()
	{
		transform.localRotation = Quaternion.identity;

		if (transform.parent == null)
			return;

		if (_bodyRenderer == null)
			_bodyRenderer = transform.parent.GetComponentInChildren<SpriteRenderer>();

		if (_bodyRenderer == null || _bodyRenderer.sprite == null)
		{
			transform.localPosition = _bodyOffset;
			return;
		}

		Vector3 center = _bodyRenderer.bounds.center;
		transform.position = new Vector3(
			center.x + _bodyOffset.x, center.y + _bodyOffset.y, transform.position.z);
	}

	void UpdateDuration()
	{
		if (!IsOn || Managers.Game.IsPlaying == false)
			return;

		float before = _remainingDuration;
		_remainingDuration = Mathf.Max(0, _remainingDuration - Time.deltaTime * _fuelScale);

		if (Mathf.Approximately(before, _remainingDuration))
			return;

		if (OnFuelChanged != null)
			OnFuelChanged.Invoke(RemainingRatio);

		if (_lowFuelWarned == false && RemainingRatio <= LowFuelRatio && _remainingDuration > 0)
		{
			_lowFuelWarned = true;
			Managers.Sound.PlayOptional(LowFuelClip, Define.Sound.Self);
		}

		if (_remainingDuration <= 0)
		{
			if (_lowFuelWarned)
			{
				_lowFuelWarned = false;
				Managers.Sound.PlayOptional(BurnedOutClip, Define.Sound.Self);
			}

			if (OnBurnedOut != null)
				OnBurnedOut.Invoke();
		}
	}

	public void SetMaxDuration(float seconds, bool refill = true)
	{
		_maxDuration = Mathf.Max(1.0f, seconds);

		if (refill)
			_remainingDuration = _maxDuration;
		else
			_remainingDuration = Mathf.Min(_remainingDuration, _maxDuration);

		if (OnFuelChanged != null)
			OnFuelChanged.Invoke(RemainingRatio);
	}

	public bool Refill(float seconds)
	{
		if (seconds <= 0 || _remainingDuration >= _maxDuration)
			return false;

		_remainingDuration = Mathf.Min(_maxDuration, _remainingDuration + seconds);

		if (OnFuelChanged != null)
			OnFuelChanged.Invoke(RemainingRatio);

		return true;
	}

	public void SnuffTo(float scale)
	{
		_snuffScale = Mathf.Clamp01(scale);
		ApplyLightSettings();
	}

	public void Toggle()
	{
		if (_isOn)
			TurnOff();
		else
			TurnOn();
	}

	public bool IsListening { get { return _listening; } }
	public float EffectiveRange { get { return _listening ? _range * _listenRangeRatio : _range; } }
	public float EffectiveAngle { get { return _listening ? _listenAngle : _angle; } }
	public float HearingScale { get { return _listening ? _listenHearingScale : 1.0f; } }

	public void SetListening(bool listening)
	{
		if (_listening == listening)
			return;

		_listening = listening;
		_fuelScale = _listening ? _listenFuelScale : 1.0f;
		ApplyLightSettings();
	}

	void ApplyLightSettings()
	{
		if (_light == null)
			return;

		float radius = _listening ? _orbRadius * _listenRangeRatio : _orbRadius;
		float flicker = Flicker();

		_light.enabled = IsOn && _snuffScale > 0.0f;
		_light.lightType = Light2D.LightType.Point;
		_light.color = _warmColor;
		_light.intensity = _intensity * (1.0f + flicker * _flickerAmount) * _snuffScale;
		_light.shadowsEnabled = true;
		_light.shadowIntensity = _shadowIntensity;
		_light.pointLightOuterRadius = radius * (1.0f + flicker * _flickerRadiusAmount) * _snuffScale;
		_light.pointLightInnerRadius = radius * _innerRangeRatio * _snuffScale;
		_light.pointLightOuterAngle = 360.0f;
		_light.pointLightInnerAngle = 360.0f;
	}

	float Flicker()
	{
		if (_flickerAmount <= 0.0f && _flickerRadiusAmount <= 0.0f)
			return 0.0f;

		float t = Time.time * _flickerSpeed;
		return Mathf.PerlinNoise(t, 0.0f) * 2.0f - 1.0f;
	}

	public bool IsInLightCone(Vector3 position)
	{
		if (IsOn == false)
			return false;

		return Vector2.Distance(transform.position, position) <= Range;
	}

	bool IsBlocked(Vector3 targetPosition)
	{
		if (_obstacleMask.value == 0)
			return false;

		RaycastHit2D hit = Physics2D.Linecast(transform.position, targetPosition, _obstacleMask);
		return hit.collider != null;
	}

	public void TurnOn()
	{
		if (_remainingDuration <= 0 || _isOn)
			return;

		_isOn = true;
		Managers.Sound.PlayAtPointOptional(
			"lantern_ignite",
			transform.position,
			Define.Sound.Ambient
		);

		if (OnToggled != null)
			OnToggled.Invoke(true);
	}

	public void TurnOff()
	{
		if (_isOn == false)
			return;

		_isOn = false;
		Managers.Sound.PlayAtPointOptional(
			"lantern_out",
			transform.position,
			Define.Sound.Ambient
		);

		if (OnToggled != null)
			OnToggled.Invoke(false);
	}
}
