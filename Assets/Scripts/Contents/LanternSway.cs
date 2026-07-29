using UnityEngine;

public class LanternSway : MonoBehaviour
{
	[SerializeField]
	Transform _owner;

	[SerializeField]
	float _swayAngle = 5.0f;

	[SerializeField]
	float _swayOffset = 0.05f;

	[SerializeField]
	float _swaysPerUnit = 0.75f;

	[SerializeField]
	float _settleSpeed = 4.0f;

	[SerializeField]
	float _moveThreshold = 0.0008f;

	Vector3 _lastPosition;
	float _phase;
	float _weight;

	void Awake()
	{
		if (_owner == null)
			_owner = transform.parent != null ? transform.parent : transform;

		_lastPosition = _owner.position;
	}

	void LateUpdate()
	{
		float moved = (_owner.position - _lastPosition).magnitude;
		_lastPosition = _owner.position;

		bool walking = moved > _moveThreshold;
		if (walking)
			_phase += moved * _swaysPerUnit * Mathf.PI * 2.0f;

		float target = walking ? 1.0f : 0.0f;
		_weight = Mathf.MoveTowards(_weight, target, _settleSpeed * Time.deltaTime);

		if (_weight <= 0.0001f)
			return;

		float wave = Mathf.Sin(_phase) * _weight;

		transform.rotation = transform.rotation * Quaternion.Euler(0, 0, wave * _swayAngle);
		transform.position += transform.up * (Mathf.Cos(_phase) * _weight * _swayOffset);
	}
}
