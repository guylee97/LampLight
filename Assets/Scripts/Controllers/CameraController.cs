using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	[SerializeField]
	Transform _target;

	[SerializeField]
	float _smoothSpeed = 10.0f;

	[SerializeField]
	Vector3 _offset = new Vector3(0, 0, -10);

	[SerializeField]
	int _pixelsPerUnit = 32;

	public Transform Target
	{
		get { return _target; }
		set { _target = value; }
	}

	public void SnapToTarget()
	{
		if (_target == null)
			return;

		_smoothed = _target.position + _offset;
		transform.position = ToPixelGrid(_smoothed);
	}

	void LateUpdate()
	{
		if (_target == null)
			return;

		Vector3 targetPosition = _target.position + _offset;

		float t = 1.0f - Mathf.Exp(-_smoothSpeed * Time.deltaTime);
		_smoothed = Vector3.Lerp(_smoothed, targetPosition, t);
		transform.position = ToPixelGrid(_smoothed);
	}

	void OnEnable()
	{
		_smoothed = transform.position;
	}

	Vector3 _smoothed;

	Vector3 ToPixelGrid(Vector3 position)
	{
		if (_pixelsPerUnit <= 0)
			return position;

		float unit = 1.0f / _pixelsPerUnit;
		return new Vector3(
			Mathf.Round(position.x / unit) * unit,
			Mathf.Round(position.y / unit) * unit,
			position.z);
	}
}
