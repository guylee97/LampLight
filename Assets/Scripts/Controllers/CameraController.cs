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

	void LateUpdate()
	{
		if (_target == null)
			return;

		Vector3 targetPosition = _target.position + _offset;
		transform.position = Vector3.Lerp(
			transform.position,
			targetPosition,
			_smoothSpeed * Time.deltaTime
		);
	}
}