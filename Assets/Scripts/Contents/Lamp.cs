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
	Light2D _light;

	[SerializeField]
	LayerMask _reactiveMask = ~0;

	[SerializeField]
	LayerMask _obstacleMask;

	[SerializeField]
	bool _followLanternInSprite = true;

	static readonly Vector2[] LanternOffsets =
	{
		new Vector2(+0.059f, 0.608f),
		new Vector2(-0.043f, 0.492f),
		new Vector2(-0.107f, 0.431f),
		new Vector2(-0.110f, 0.410f),
		new Vector2(+0.008f, 0.495f),
		new Vector2(+0.061f, 0.505f),
		new Vector2(+0.139f, 0.437f),
		new Vector2(+0.078f, 0.591f),
	};

	float _remainingDuration;
	IFacingSource _facingSource;
	Component _directionOwner;
	readonly HashSet<ILampReactive> _illuminatedTargets = new HashSet<ILampReactive>();
	readonly HashSet<ILampReactive> _currentTargets = new HashSet<ILampReactive>();
	readonly Collider2D[] _hits = new Collider2D[32];
	ContactFilter2D _reactiveFilter;

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
		_facingSource = GetComponentInParent<IFacingSource>();
		_directionOwner = _facingSource as Component;

		if (_light == null)
			_light = GetComponent<Light2D>();

		if (_light == null)
			_light = gameObject.AddComponent<Light2D>();

		_reactiveFilter = new ContactFilter2D();
		_reactiveFilter.useLayerMask = true;
		_reactiveFilter.layerMask = _reactiveMask;
		_reactiveFilter.useTriggers = true;

		ApplyLightSettings();
	}

	void Update()
	{
		UpdateDirection();
		UpdateLanternOffset();
		UpdateDuration();
		ApplyLightSettings();
	}

	void UpdateLanternOffset()
	{
		if (!_followLanternInSprite)
			return;

		PlayerController player = _directionOwner as PlayerController;
		DefaultEnemy enemy = _directionOwner as DefaultEnemy;

		int index = player != null ? (int)player.Direction
			: enemy != null ? (int)enemy.Direction
			: -1;

		if (index < 0 || index >= LanternOffsets.Length)
			return;

		Vector2 offset = LanternOffsets[index];
		transform.localPosition = new Vector3(offset.x, offset.y, transform.localPosition.z);
	}

	void FixedUpdate()
	{
		UpdateReactiveTargets();
	}

	void UpdateDirection()
	{
		if (_facingSource != null)
		{
			Vector2 facing = _facingSource.Facing;
			if (facing.sqrMagnitude > Mathf.Epsilon)
				transform.up = facing;

			return;
		}

		if (transform.parent != null)
			transform.rotation = transform.parent.rotation;
	}

	void UpdateDuration()
	{
		if (!IsOn || Managers.Game.IsPlaying == false)
			return;

		float before = _remainingDuration;
		_remainingDuration = Mathf.Max(0, _remainingDuration - Time.deltaTime);

		if (Mathf.Approximately(before, _remainingDuration))
			return;

		if (OnFuelChanged != null)
			OnFuelChanged.Invoke(RemainingRatio);

		if (_remainingDuration <= 0 && OnBurnedOut != null)
			OnBurnedOut.Invoke();
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

	void ApplyLightSettings()
	{
		if (_light == null)
			return;

		_light.enabled = IsOn;
		_light.lightType = Light2D.LightType.Point;
		_light.intensity = _intensity;
		_light.shadowsEnabled = true;
		_light.shadowIntensity = _shadowIntensity;
		_light.pointLightOuterRadius = _range;
		_light.pointLightInnerRadius = _range * _innerRangeRatio;
		_light.pointLightOuterAngle = _angle;
		_light.pointLightInnerAngle = _angle * _innerAngleRatio;
	}

	public bool IsInLightCone(Vector3 position)
	{
		if (!IsOn)
			return false;

		Vector2 toTarget = position - transform.position;

		if (toTarget.magnitude > _range)
			return false;

		float angleToTarget = Vector2.Angle(transform.up, toTarget.normalized);
		return angleToTarget <= _angle * 0.5f;
	}

	void UpdateReactiveTargets()
	{
		_currentTargets.Clear();

		if (IsOn && Managers.Game.IsPlaying)
		{
			int count = Physics2D.OverlapCircle(transform.position, _range, _reactiveFilter, _hits);
			for (int i = 0; i < count; i++)
			{
				Collider2D hit = _hits[i];
				if (hit == null)
					continue;

				ILampReactive target = FindReactiveTarget(hit);
				if (target == null || _currentTargets.Contains(target))
					continue;

				Vector3 targetPosition = hit.bounds.center;
				if (!IsInLightCone(targetPosition) || IsBlocked(targetPosition))
					continue;

				_currentTargets.Add(target);
			}
		}

		foreach (ILampReactive target in _currentTargets)
		{
			if (_illuminatedTargets.Add(target))
				target.OnLampEnter();

			target.OnLampStay();
		}

		foreach (ILampReactive target in _illuminatedTargets)
		{
			if (!_currentTargets.Contains(target))
				target.OnLampExit();
		}

		_illuminatedTargets.RemoveWhere(target => !_currentTargets.Contains(target));
	}

	ILampReactive FindReactiveTarget(Collider2D hit)
	{
		return hit.GetComponentInParent<ILampReactive>();
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

		if (OnToggled != null)
			OnToggled.Invoke(true);
	}

	public void TurnOff()
	{
		if (_isOn == false)
			return;

		_isOn = false;

		if (OnToggled != null)
			OnToggled.Invoke(false);
	}
}
