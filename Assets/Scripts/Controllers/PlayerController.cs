using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : BaseController
{
	[SerializeField]
	float _walkSpeed = 4.0f;

	[SerializeField]
	float _runSpeed = 6.5f;

	[SerializeField]
	float _sneakSpeed = 2.0f;

	[SerializeField]
	float _rotationSpeed = 15.0f;

	[SerializeField]
	float _walkFootstepInterval = 0.45f;

	[SerializeField]
	float _runFootstepInterval = 0.32f;

	[SerializeField]
	float _sneakFootstepInterval = 0.75f;

	[SerializeField]
	float _walkNoiseRadius = 4.0f;

	[SerializeField]
	float _runNoiseRadius = 8.0f;

	[SerializeField]
	float _sneakNoiseRadius = 1.5f;

	[SerializeField]
	AudioClip[] _footstepClips;

	Rigidbody2D _rigidbody;
	PlayerStatus _status;
	float _nextFootstepTime;
	float _moveSpeed;
	Vector2 _moveDir;
	bool _initialized;

	public float CurrentNoiseRadius { get; private set; }

	public override Define.State State
	{
		get { return _state; }
		set { _state = value; }
	}

	void Awake()
	{
		Init();
	}

	public override void Init()
	{
		if (_initialized)
			return;

		_initialized = true;
		WorldObjectType = Define.WorldObject.Player;
		_status = GetComponent<PlayerStatus>();
		_rigidbody = GetComponent<Rigidbody2D>();

		if (_rigidbody == null)
			_rigidbody = gameObject.AddComponent<Rigidbody2D>();

		_rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
		_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
	}

	void FixedUpdate()
	{
		Move();
		Rotate();
	}

	protected override void UpdateIdle()
	{
		UpdateMovement();
	}

	protected override void UpdateMoving()
	{
		UpdateMovement();
	}

	void UpdateMovement()
	{
		Keyboard keyboard = Keyboard.current;
		if (keyboard == null)
			return;

		float horizontal = GetHorizontalInput(keyboard);
		float vertical = GetVerticalInput(keyboard);

		_moveDir = new Vector2(horizontal, vertical).normalized;
		bool isMoving = _moveDir.sqrMagnitude > 0.01f;
		bool wantsToRun = keyboard.leftShiftKey.isPressed && (_status == null || _status.CanRun);
		bool wantsToSneak = keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed;

		_moveSpeed = _walkSpeed;
		float footstepInterval = _walkFootstepInterval;
		CurrentNoiseRadius = _walkNoiseRadius;

		if (isMoving && wantsToRun && !wantsToSneak)
		{
			_moveSpeed = _runSpeed;
			footstepInterval = _runFootstepInterval;
			CurrentNoiseRadius = _runNoiseRadius;

			if (_status != null)
				_status.ConsumeRunStamina(Time.deltaTime);
		}
		else
		{
			if (isMoving && wantsToSneak)
			{
				_moveSpeed = _sneakSpeed;
				footstepInterval = _sneakFootstepInterval;
				CurrentNoiseRadius = _sneakNoiseRadius;
			}

			if (_status != null)
				_status.RecoverStamina(Time.deltaTime);
		}

		if (!isMoving)
		{
			_moveSpeed = 0;
			CurrentNoiseRadius = 0;
			State = Define.State.Idle;
			return;
		}

		PlayFootstep(footstepInterval);

		State = Define.State.Moving;
	}

	void Move()
	{
		if (_moveDir.sqrMagnitude <= 0.01f || _moveSpeed <= 0)
			return;

		Vector2 movement = _moveDir * _moveSpeed * Time.fixedDeltaTime;

		if (_rigidbody != null)
			_rigidbody.MovePosition(_rigidbody.position + movement);
		else
			transform.position += (Vector3)movement;
	}

	float GetHorizontalInput(Keyboard keyboard)
	{
		float value = 0;

		if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
			value -= 1;

		if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
			value += 1;

		return value;
	}

	float GetVerticalInput(Keyboard keyboard)
	{
		float value = 0;

		if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
			value -= 1;

		if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
			value += 1;

		return value;
	}

	void Rotate()
	{
		if (_moveDir.sqrMagnitude <= 0.01f || _rigidbody == null)
			return;

		float targetAngle = Mathf.Atan2(_moveDir.y, _moveDir.x) * Mathf.Rad2Deg - 90.0f;
		float nextAngle = Mathf.LerpAngle(
			_rigidbody.rotation,
			targetAngle,
			_rotationSpeed * Time.fixedDeltaTime
		);

		_rigidbody.MoveRotation(nextAngle);
	}

	void PlayFootstep(float interval)
	{
		if (_footstepClips == null || _footstepClips.Length == 0)
			return;

		if (Time.time < _nextFootstepTime)
			return;

		AudioClip clip = _footstepClips[Random.Range(0, _footstepClips.Length)];
		Managers.Sound.PlayAtPoint(clip, transform.position);
		_nextFootstepTime = Time.time + interval;
	}
}
