using System.Collections;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, ILampReactive
{
	[SerializeField]
	protected Define.EnemyState _state;

	[SerializeField]
	AudioClip _idleSound;

	[SerializeField]
	AudioClip _patrolSound;

	[SerializeField]
	AudioClip _chaseSound;

	[SerializeField]
	float _chaseSignalInterval = 0.65f;

	[SerializeField]
	float _chaseSoundInterval = 2.0f;

	[SerializeField, Range(0.0f, 1.0f)]
	float _chaseSoundVolume = 1.0f;

	[SerializeField]
	float _audibleDistance = 4.0f;

	[SerializeField]
	float _presenceSoundInterval = 5.0f;

	Animator _animator;
	AudioSource _stateAudioSource;
	Coroutine _slowCoroutine;
	float _speedMultiplier = 1.0f;
	float _nextChaseSignalTime;
	float _nextChaseSoundTime;
	float _nextPresenceSoundTime;

	public Define.WorldObject WorldObjectType { get; protected set; } = Define.WorldObject.Enemy;
	protected float SpeedMultiplier { get { return _speedMultiplier; } }

	public Define.EnemyState State
	{
		get { return _state; }
		protected set
		{
			if (_state == value)
				return;

			_state = value;
			UpdateStateSound();
			UpdateAnimatorState();
		}
	}

	void Start()
	{
		Util.GetOrAddComponent<WorldYSort>(gameObject);

		Init();
		UpdateStateSound();
		UpdateAnimatorState();
	}

	void Update()
	{
		switch (State)
		{
			case Define.EnemyState.Idle:
				UpdateIdle();
				break;
			case Define.EnemyState.Patrol:
				UpdatePatrol();
				break;
			case Define.EnemyState.Chasing:
				UpdateChasing();
				break;
			case Define.EnemyState.Caught:
				UpdateCaught();
				break;
			case Define.EnemyState.Die:
				UpdateDie();
				break;
		}

		UpdateChaseSoundSignal();
		UpdatePresenceSound();
		UpdateChaseSound();
	}

	public abstract void Init();

	protected virtual void UpdateIdle() { }
	protected virtual void UpdatePatrol() { }
	protected virtual void UpdateChasing() { }
	protected virtual void UpdateCaught() { }
	protected virtual void UpdateDie() { }

	public void ApplySlow(float speedMultiplier, float duration)
	{
		if (_slowCoroutine != null)
			StopCoroutine(_slowCoroutine);

		_slowCoroutine = StartCoroutine(SlowRoutine(speedMultiplier, duration));
	}

	IEnumerator SlowRoutine(float speedMultiplier, float duration)
	{
		_speedMultiplier = Mathf.Clamp01(speedMultiplier);
		yield return new WaitForSeconds(duration);
		_speedMultiplier = 1.0f;
		_slowCoroutine = null;
	}

	protected void UpdateAnimatorDirection(Vector2 direction)
	{
		if (_animator == null)
			_animator = GetComponent<Animator>();

		if (_animator == null || direction.sqrMagnitude <= 0.01f)
			return;

		_animator.SetFloat("MoveX", direction.x);
		_animator.SetFloat("MoveY", direction.y);
	}

	void UpdateAnimatorState()
	{
		if (_animator == null)
			_animator = GetComponent<Animator>();

		if (_animator == null)
			return;

		bool isChasing = State == Define.EnemyState.Chasing;
		bool isMoving = State == Define.EnemyState.Patrol || isChasing;
		_animator.SetBool("IsMoving", isMoving);
		_animator.SetBool("IsChasing", isChasing);
	}

	public virtual void OnLampEnter()
	{
		Debug.Log($"{name}: Lamp entered.");
	}

	public virtual void OnLampStay() { }

	public virtual void OnLampExit()
	{
		Debug.Log($"{name}: Lamp exited.");
	}

	protected virtual void OnCollisionEnter2D(Collision2D collision)
	{
		TryCatchPlayer(collision.gameObject);
	}

	protected virtual void OnTriggerEnter2D(Collider2D other)
	{
		TryCatchPlayer(other.gameObject);
	}

	protected static bool CanCatchPlayer { get { return DebugOverlay.Invulnerable == false; } }

	void TryCatchPlayer(GameObject target)
	{
		if (CanCatchPlayer == false)
			return;

		if (State == Define.EnemyState.Die || State == Define.EnemyState.Caught)
			return;

		if (target.GetComponentInParent<PlayerController>() == null)
			return;

		State = Define.EnemyState.Caught;
	}

	void UpdateStateSound()
	{
		AudioClip clip = null;

		if (State == Define.EnemyState.Chasing)
		{
			Managers.Sound.EmitSoundSignal(transform.position, Define.Sound.Threat, 0.85f);
			_nextChaseSignalTime = Time.time + _chaseSignalInterval;
			PlayChaseSound();
		}

		switch (State)
		{
			case Define.EnemyState.Idle:
				clip = _idleSound;
				break;
			case Define.EnemyState.Patrol:
				clip = _patrolSound;
				break;
		}

		if (clip == null)
		{
			if (_stateAudioSource != null)
				_stateAudioSource.Stop();
			return;
		}

		if (_stateAudioSource == null)
		{
			_stateAudioSource = GetComponent<AudioSource>();
			if (_stateAudioSource == null)
				_stateAudioSource = gameObject.AddComponent<AudioSource>();

			_stateAudioSource.loop = false;
			Managers.Sound.ConfigureSource(_stateAudioSource, Define.Sound.Threat, true);
		}

		_stateAudioSource.clip = clip;
		_stateAudioSource.maxDistance = _audibleDistance;
		_stateAudioSource.Play();
		_nextPresenceSoundTime = Time.time + Mathf.Max(clip.length, _presenceSoundInterval);
	}

	void UpdatePresenceSound()
	{
		if (State != Define.EnemyState.Idle && State != Define.EnemyState.Patrol)
			return;

		if (_stateAudioSource == null || _stateAudioSource.clip == null)
			return;

		if (_stateAudioSource.isPlaying || Time.time < _nextPresenceSoundTime)
			return;

		_stateAudioSource.Play();
		_nextPresenceSoundTime = Time.time
			+ Mathf.Max(_stateAudioSource.clip.length, _presenceSoundInterval);
	}

	void UpdateChaseSoundSignal()
	{
		if (State != Define.EnemyState.Chasing || Time.time < _nextChaseSignalTime)
			return;

		Managers.Sound.EmitSoundSignal(transform.position, Define.Sound.Threat, 0.85f);
		_nextChaseSignalTime = Time.time + _chaseSignalInterval;
	}

	void UpdateChaseSound()
	{
		if (State != Define.EnemyState.Chasing || Time.time < _nextChaseSoundTime)
			return;

		PlayChaseSound();
	}

	void PlayChaseSound()
	{
		if (_chaseSound != null)
			Managers.Sound.PlayAtPoint(
				_chaseSound,
				transform.position,
				Define.Sound.Threat,
				_chaseSoundVolume,
				1.0f,
				_audibleDistance
			);

		_nextChaseSoundTime = Time.time + Mathf.Max(0.1f, _chaseSoundInterval);
	}
}
