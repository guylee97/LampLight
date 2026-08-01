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
	float _walkNoiseRadius = 5.0f;

	[SerializeField]
	float _runNoiseRadius = 9.0f;

	[SerializeField]
	float _sneakNoiseRadius = 2.0f;

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

	[SerializeField]
	AudioClip[] _noisyFloorFootstepClips;

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
	bool _isListening;
	float _noisePulseRadius;
	float _noisePulseUntil;

	public float CurrentNoiseRadius { get; private set; }
	public Vector2 FacingDirection { get; private set; } = Vector2.down;

	public Lamp Lamp { get { return _lamp; } }

	public bool IsSneaking { get { return _sneaking; } }

	public bool IsOnNoisyFloor { get { return _onNoisyFloor; } }

	public bool IsListening { get { return _isListening; } }

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
		_rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

		if (_rigidbody.sharedMaterial == null)
		{
			PhysicsMaterial2D material = new PhysicsMaterial2D("PlayerFrictionless");
			material.friction = 0;
			material.bounciness = 0;
			_rigidbody.sharedMaterial = material;
		}
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
			SetListening(false);
			Managers.Sound.SetRunning(false);
			State = Define.State.Idle;
			return;
		}

		base.Update();
		UpdateLampInput();
		UpdateListeningInput();
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

	void UpdateListeningInput()
	{
		Mouse mouse = Mouse.current;
		SetListening(mouse != null && mouse.middleButton.isPressed);
	}

	void SetListening(bool listening)
	{
		if (_isListening == listening)
			return;

		_isListening = listening;
		Managers.Sound.SetListening(listening);
		Managers.Sound.PlayOptional(
			listening ? "listen_enter" : "listen_exit",
			Define.Sound.UI
		);
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
		bool isRunning = isMoving && wantsToRun && !wantsToSneak;

		_sneaking = wantsToSneak;
		Managers.Sound.SetRunning(isRunning);

		_moveSpeed = _walkSpeed;
		float footstepInterval = _walkFootstepInterval;
		AudioClip[] footstepClips = _walkFootstepClips;
		float noiseRadius = _walkNoiseRadius;

		if (isRunning)
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
			{
				noiseRadius *= _noisyFloorNoiseScale;
				if (_noisyFloorFootstepClips != null && _noisyFloorFootstepClips.Length > 0)
					footstepClips = _noisyFloorFootstepClips;
			}

			FacingDirection = _moveDir;
			UpdateAnimatorDirection();
			PlayFootstep(footstepInterval, footstepClips, noiseRadius / 9.0f);
			State = Define.State.Moving;
			UpdateAnimatorMovement(true);
		}
		else
		{
			_moveSpeed = 0;
			noiseRadius = 0;
			State = Define.State.Idle;
			UpdateAnimatorMovement(false);
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

	void UpdateAnimatorMovement(bool isMoving)
	{
		if (_animator != null)
			_animator.SetBool("IsMoving", isMoving);
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

	void PlayFootstep(float interval, AudioClip[] footstepClips, float intensity)
	{
		if (Time.time < _nextFootstepTime)
			return;

		if (footstepClips == null || footstepClips.Length == 0)
		{
			Managers.Sound.EmitSoundSignal(transform.position, Define.Sound.Self, intensity);
		}
		else
		{
			AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
			Managers.Sound.PlayAtPoint(clip, transform.position, Define.Sound.Self);
		}

		_nextFootstepTime = Time.time + interval;
	}
}
