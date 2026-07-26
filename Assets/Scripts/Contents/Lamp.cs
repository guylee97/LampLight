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

	float _remainingDuration;
	readonly HashSet<ILampReactive> _illuminatedTargets = new HashSet<ILampReactive>();
	readonly HashSet<ILampReactive> _currentTargets = new HashSet<ILampReactive>();

	public bool IsOn { get { return _isOn && _remainingDuration > 0; } }
	public float Range { get { return _range; } }
	public float Angle { get { return _angle; } }
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
		UpdateDirectionToParent();
		UpdateDuration();
		ApplyLightSettings();
	}

	void FixedUpdate()
	{
		UpdateReactiveTargets();
	}

	void UpdateDirectionToParent()
	{
		if (transform.parent == null)
			return;

		transform.rotation = transform.parent.rotation;
	}

	void UpdateDuration()
	{
		if (!IsOn)
			return;

		_remainingDuration = Mathf.Max(0, _remainingDuration - Time.deltaTime);
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

		if (IsOn)
		{
			Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _range, _reactiveMask);
			foreach (Collider2D hit in hits)
			{
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
		MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
		foreach (MonoBehaviour behaviour in behaviours)
		{
			if (behaviour is ILampReactive reactive)
				return reactive;
		}

		return null;
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
		if (_remainingDuration > 0)
			_isOn = true;
	}

	public void TurnOff()
	{
		_isOn = false;
	}
}
