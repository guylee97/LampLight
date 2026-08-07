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
	float _focusSpeed = 14.0f;

	Camera _camera;
	Transform _focus;
	float _focusWeight;
	float _focusTarget;
	float _shakeAmplitude;
	float _shakeUntil;
	float _shakeDuration;
	float _baseOrthographicSize = -1.0f;
	float _zoomScale = 1.0f;
	float _zoomTarget = 1.0f;
	float _zoomSpeed = 6.0f;

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

	public void Shake(float amplitude, float duration)
	{
		_shakeAmplitude = Mathf.Max(_shakeAmplitude, amplitude);
		_shakeDuration = Mathf.Max(0.01f, duration);
		_shakeUntil = Time.unscaledTime + _shakeDuration;
	}

	public void FocusOn(Transform focus, float weight, float speed = 14.0f)
	{
		_focus = focus;
		_focusTarget = Mathf.Clamp01(weight);
		_focusSpeed = speed;
	}

	public void ZoomTo(float scale, float speed = 6.0f)
	{
		_zoomTarget = Mathf.Max(0.05f, scale);
		_zoomSpeed = speed;
	}

	public void ResetEffects()
	{
		_focus = null;
		_focusTarget = 0.0f;
		_focusWeight = 0.0f;
		_shakeAmplitude = 0.0f;
		_shakeUntil = 0.0f;
		_zoomTarget = 1.0f;
		_zoomScale = 1.0f;
		ApplyZoom();
	}

	void Awake()
	{
		_camera = GetComponent<Camera>();

		if (_camera != null && _camera.orthographic)
			_baseOrthographicSize = _camera.orthographicSize;
	}

	void LateUpdate()
	{
		UpdateZoom();

		if (_target == null)
			return;

		Vector3 anchor = _target.position;

		_focusWeight = Mathf.MoveTowards(
			_focusWeight,
			_focus == null ? 0.0f : _focusTarget,
			_focusSpeed * Time.unscaledDeltaTime);

		if (_focus != null && _focusWeight > 0.0f)
			anchor = Vector3.Lerp(anchor, _focus.position, _focusWeight);

		Vector3 targetPosition = anchor + _offset;

		float t = 1.0f - Mathf.Exp(-_smoothSpeed * Time.deltaTime);

		if (_focusWeight > 0.0f)
			t = 1.0f - Mathf.Exp(-_smoothSpeed * Time.unscaledDeltaTime);

		transform.position = Vector3.Lerp(transform.position, targetPosition, t);
		transform.position += ShakeOffset();
	}

	Vector3 ShakeOffset()
	{
		if (Time.unscaledTime >= _shakeUntil || _shakeAmplitude <= 0.0f)
		{
			_shakeAmplitude = 0.0f;
			return Vector3.zero;
		}

		float remaining = Mathf.Clamp01((_shakeUntil - Time.unscaledTime) / _shakeDuration);
		float amplitude = _shakeAmplitude * remaining;
		float seed = Time.unscaledTime * 47.0f;

		float x = (Mathf.PerlinNoise(seed, 0.0f) * 2.0f - 1.0f) * amplitude;
		float y = (Mathf.PerlinNoise(0.0f, seed) * 2.0f - 1.0f) * amplitude;
		return new Vector3(x, y, 0.0f);
	}

	void UpdateZoom()
	{
		if (Mathf.Approximately(_zoomScale, _zoomTarget))
			return;

		_zoomScale = Mathf.MoveTowards(
			_zoomScale,
			_zoomTarget,
			_zoomSpeed * Time.unscaledDeltaTime);

		ApplyZoom();
	}

	void ApplyZoom()
	{
		if (_camera == null || _baseOrthographicSize <= 0.0f)
			return;

		_camera.orthographicSize = _baseOrthographicSize * _zoomScale;
	}
}
