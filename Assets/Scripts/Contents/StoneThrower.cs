using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class StoneThrower : MonoBehaviour
{
	[SerializeField]
	int _stones;

	[SerializeField]
	float _maxRangeTiles = 8.0f;

	[SerializeField]
	float _flightSeconds = 0.45f;

	[SerializeField]
	float _landingNoiseRadius = 8.0f;

	[SerializeField]
	float _landingNoiseDuration = 1.2f;

	[SerializeField]
	PlayerController _player;

	bool _wasPressed;

	public Action<int> OnStonesChanged;

	public int Stones { get { return _stones; } }

	public static Vector3 ClampTarget(Vector3 origin, Vector3 desired, float maxRange)
	{
		Vector3 offset = desired - origin;
		offset.z = 0.0f;

		if (offset.magnitude <= maxRange)
			return origin + offset;

		return origin + offset.normalized * maxRange;
	}

	public void SetStones(int count)
	{
		_stones = Mathf.Max(0, count);

		if (OnStonesChanged != null)
			OnStonesChanged.Invoke(_stones);
	}

	void Awake()
	{
		if (_player == null)
			_player = GetComponent<PlayerController>();
	}

	void Update()
	{
		if (Managers.Game.IsPlaying == false)
			return;

		Mouse mouse = Mouse.current;
		if (mouse == null)
			return;

		bool pressed = mouse.leftButton.isPressed;

		if (pressed && _wasPressed == false)
			TryThrow(mouse.position.ReadValue());

		_wasPressed = pressed;
	}

	void TryThrow(Vector2 screenPosition)
	{
		if (_stones <= 0)
			return;

		Camera camera = Camera.main;
		if (camera == null)
			return;

		Vector3 world = camera.ScreenToWorldPoint(screenPosition);
		world.z = 0.0f;

		Vector3 target = ClampTarget(transform.position, world, _maxRangeTiles);

		SetStones(_stones - 1);
		Managers.Sound.PlayAtPointOptional("stone_throw", transform.position, Define.Sound.Self, 4.0f);
		StartCoroutine(Land(target));
	}

	IEnumerator Land(Vector3 target)
	{
		yield return new WaitForSeconds(_flightSeconds);

		Managers.Sound.PlayAtPointOptional("stone_land", target, Define.Sound.Self, _landingNoiseRadius);
		// 소리 방향 HUD 기능 제거.
		// SoundRing.Emit(target - transform.position, SoundRing.SelfColor, _landingNoiseRadius);

		NoiseLure lure = NoiseLure.Spawn(target, _landingNoiseRadius, _landingNoiseDuration);
		if (lure == null)
			yield break;
	}
}
