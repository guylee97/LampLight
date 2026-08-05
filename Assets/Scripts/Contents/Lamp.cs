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
	float _innerRangeRatio = 0.65f;

	[SerializeField]
	float _innerAngleRatio = 0.8f;

	[SerializeField]
	float _orbRadius = 1.4f;

	[SerializeField]
	float _forwardOffset = 1.2f;

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
	public float Range { get { return _range; } }
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
		UpdateDirection();
		ApplyLightSettings();
	}

	void FixedUpdate()
	{
	}

	void UpdateDirection()
	{
		if (transform.parent == null)
			return;

		PlayerController player = transform.parent.GetComponent<PlayerController>();
		if (player == null)
			return;

		Vector2 direction = player.FacingDirection;
		if (direction.sqrMagnitude <= 0.01f)
			return;

		direction.Normalize();
		transform.localPosition = direction * _forwardOffset;

		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90.0f;
		transform.localRotation = Quaternion.Euler(0, 0, angle);
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

		_light.enabled = IsOn;
		_light.lightType = Light2D.LightType.Point;
		_light.intensity = _intensity;
		_light.shadowsEnabled = true;
		_light.shadowIntensity = _shadowIntensity;
		_light.pointLightOuterRadius = radius;
		_light.pointLightInnerRadius = radius * _innerRangeRatio;
		_light.pointLightOuterAngle = 360.0f;
		_light.pointLightInnerAngle = 360.0f;

		/*
		// 기존 원뿔형 등불 설정. 필요할 때 위의 원형 설정 대신 복구한다.
		float range = EffectiveRange;
		float angle = EffectiveAngle;
		_light.pointLightOuterRadius = range;
		_light.pointLightInnerRadius = range * _innerRangeRatio;
		_light.pointLightOuterAngle = angle;
		_light.pointLightInnerAngle = angle * _innerAngleRatio;
		*/
	}

	public bool IsInLightCone(Vector3 position)
	{
		if (!IsOn)
			return false;

		float radius = _listening ? _orbRadius * _listenRangeRatio : _orbRadius;
		return Vector2.Distance(transform.position, position) <= radius;
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
