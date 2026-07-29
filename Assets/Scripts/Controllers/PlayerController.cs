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
	Lamp _lamp;

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
	float _noisyFloorNoiseScale = 1.8f;

	[SerializeField]
	float _lampVisibilityBonus = 0.65f;

	[SerializeField]
	float _sneakVisibilityScale = 0.7f;

	[SerializeField]
	AudioClip[] _walkFootstepClips;

	[SerializeField]
	AudioClip[] _runFootstepClips;

	[SerializeField]
	AudioClip[] _sneakFootstepClips;

	Rigidbody2D _rigidbody;
	PlayerStatus _status;
	Animator _animator;
	float _nextFootstepTime;
	float _moveSpeed;
	Vector2 _moveDir;
	bool _initialized;
	bool _sneaking;
	bool _onNoisyFloor;
	bool _lampKeyWasPressed;
	float _noisePulseRadius;
	float _noisePulseUntil;

	public float CurrentNoiseRadius { get; private set; }
	public Vector2 FacingDirection { get; private set; } = Vector2.down;

	public Lamp Lamp { get { return _lamp; } }

	public bool IsSneaking { get { return _sneaking; } }

	public bool IsOnNoisyFloor { get { return _onNoisyFloor; } }

	public PlayerStatus Status { get { return _status; } }

	public float VisibilityScale
	{
		get
		{
			float scale = 1.0f;

			if (_lamp != null && _lamp.IsOn)
				scale += _lampVisibilityBonus;

			if (_sneaking)
				scale *= _sneakVisibilityScale;

			return scale;
		}
	}

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
		_animator = GetComponent<Animator>();

		if (_lamp == null)
			_lamp = GetComponentInChildren<Lamp>();

		if (_rigidbody == null)
			_rigidbody = gameObject.AddComponent<Rigidbody2D>();

		_rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
		_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
	}

	void FixedUpdate()
	{
		Move();
	}

	protected override void Update()
	{
		if (Managers.Game.IsPlaying == false)
		{
			_moveDir = Vector2.zero;
			_moveSpeed = 0;
			CurrentNoiseRadius = 0;
			State = Define.State.Idle;
			return;
		}

		base.Update();
		UpdateLampInput();
	}

	protected override void UpdateIdle()
	{
		UpdateMovement();
	}

	protected override void UpdateMoving()
	{
		UpdateMovement();
	}

	void UpdateLampInput()
	{
		Keyboard keyboard = Keyboard.current;
		if (keyboard == null || _lamp == null)
			return;

		bool pressed = keyboard.fKey.isPressed;
		if (pressed && _lampKeyWasPressed == false)
			_lamp.Toggle();

		_lampKeyWasPressed = pressed;
	}

	public void EmitNoise(float radius, float duration)
	{
		if (radius <= _noisePulseRadius && Time.time < _noisePulseUntil)
			return;

		_noisePulseRadius = radius;
		_noisePulseUntil = Time.time + duration;
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

		_sneaking = wantsToSneak;

		_moveSpeed = _walkSpeed;
		float footstepInterval = _walkFootstepInterval;
		AudioClip[] footstepClips = _walkFootstepClips;
		float noiseRadius = _walkNoiseRadius;

		if (isMoving && wantsToRun && !wantsToSneak)
		{
			_moveSpeed = _runSpeed;
			footstepInterval = _runFootstepInterval;
			footstepClips = _runFootstepClips;
			noiseRadius = _runNoiseRadius;

			if (_status != null)
				_status.ConsumeRunStamina(Time.deltaTime);
		}
		else
		{
			if (isMoving && wantsToSneak)
			{
				_moveSpeed = _sneakSpeed;
				footstepInterval = _sneakFootstepInterval;
				footstepClips = _sneakFootstepClips;
				noiseRadius = _sneakNoiseRadius;
			}

			if (_status != null)
				_status.RecoverStamina(Time.deltaTime);
		}

		_onNoisyFloor = MapCoord.IsNoisy(transform.position);

		if (isMoving)
		{
			if (_onNoisyFloor)
				noiseRadius *= _noisyFloorNoiseScale;

			FacingDirection = _moveDir;
			UpdateAnimatorDirection();
			PlayFootstep(footstepInterval, footstepClips);
			State = Define.State.Moving;
		}
		else
		{
			_moveSpeed = 0;
			noiseRadius = 0;
			State = Define.State.Idle;
		}

		CurrentNoiseRadius = Time.time < _noisePulseUntil
			? Mathf.Max(noiseRadius, _noisePulseRadius)
			: noiseRadius;
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

	void UpdateAnimatorDirection()
	{
		if (_animator == null)
			return;

		_animator.SetFloat("MoveX", FacingDirection.x);
		_animator.SetFloat("MoveY", FacingDirection.y);
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

	void PlayFootstep(float interval, AudioClip[] footstepClips)
	{
		if (footstepClips == null || footstepClips.Length == 0)
			return;

		if (Time.time < _nextFootstepTime)
			return;

		AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
		Managers.Sound.PlayAtPoint(clip, transform.position);
		_nextFootstepTime = Time.time + interval;
	}
}
