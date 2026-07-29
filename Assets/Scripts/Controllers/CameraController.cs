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

	public Transform Target
	{
		get { return _target; }
		set { _target = value; }
	}

	public void SnapToTarget()
	{
		if (_target == null)
			return;

		transform.position = _target.position + _offset;
	}

	void LateUpdate()
	{
		if (_target == null)
			return;

		Vector3 targetPosition = _target.position + _offset;

		float t = 1.0f - Mathf.Exp(-_smoothSpeed * Time.deltaTime);
		transform.position = Vector3.Lerp(transform.position, targetPosition, t);
	}
}
