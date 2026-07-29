using UnityEngine;

public class WalkBob : MonoBehaviour
{
	[SerializeField]
	Transform _visual;

	[SerializeField]
	float _amplitude = 0.14f;

	[SerializeField]
	float _stepsPerUnit = 1.5f;

	[SerializeField]
	float _settleSpeed = 10.0f;

	[SerializeField]
	float _moveThreshold = 0.0008f;

	Vector3 _restPosition;
	Vector3 _lastPosition;
	float _phase;
	float _height;

	void Awake()
	{
		if (_visual == null)
			_visual = transform;

		_restPosition = _visual.localPosition;
		_lastPosition = transform.position;
	}

	void LateUpdate()
	{
		float moved = (transform.position - _lastPosition).magnitude;
		_lastPosition = transform.position;

		if (moved > _moveThreshold)
		{
			_phase += moved * _stepsPerUnit * Mathf.PI * 2.0f;

			float s = Mathf.Sin(_phase);
			_height = s * s * _amplitude;
		}
		else
		{
			_height = Mathf.MoveTowards(_height, 0, _settleSpeed * _amplitude * Time.deltaTime);
			_phase = 0;
		}

		_visual.localPosition = _restPosition + new Vector3(0, _height, 0);
	}
}
