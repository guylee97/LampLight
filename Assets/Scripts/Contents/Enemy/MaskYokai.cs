using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MaskYokai : EnemyBase
{
	public const string PetrifyClip = "yokai_petrify";
	public const string ReleaseClip = "yokai_release";

	[SerializeField]
	float _patrolSpeed = 1.6f;

	[SerializeField]
	float _chaseSpeed = 3.4f;

	[SerializeField]
	float _searchSpeed = 2.2f;

	[SerializeField, Range(0.1f, 1.0f)]
	float _hearingScale = 0.55f;

	[SerializeField]
	float _sightRange = 7.5f;

	[SerializeField]
	float _catchRange = 0.55f;

	[SerializeField]
	float _arrivalRange = 0.35f;

	[SerializeField]
	float _patrolRetargetSeconds = 5.0f;

	[SerializeField]
	float _ritualSpeedBonusPerStep = 0.35f;

	Rigidbody2D _rigidbody;
	PlayerController _player;
	Vector2 _moveDir;
	Vector2 _lastKnownPosition;
	Vector2 _patrolTarget;
	Define.EnemyState _stateBeforePetrify = Define.EnemyState.Patrol;
	float _nextPatrolRetargetTime;
	bool _hasPatrolTarget;
	bool _wasPetrified;

	public override void Init()
	{
		WorldObjectType = Define.WorldObject.Enemy;
		_rigidbody = GetComponent<Rigidbody2D>();

		if (_rigidbody == null)
			_rigidbody = gameObject.AddComponent<Rigidbody2D>();

		_rigidbody.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody.gravityScale = 0;
		_rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
		_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

		if (GetComponent<Collider2D>() == null)
			gameObject.AddComponent<CircleCollider2D>();

		State = Define.EnemyState.Patrol;
	}

	void FixedUpdate()
	{
		if (_moveDir.sqrMagnitude <= 0.01f)
			return;

		Vector2 movement = _moveDir * CurrentSpeed * SpeedMultiplier * Time.fixedDeltaTime;
		_rigidbody.MovePosition(_rigidbody.position + movement);
	}

	float CurrentSpeed
	{
		get
		{
			float ritualBonus = 1.0f + Altar.CompletedSteps * _ritualSpeedBonusPerStep;

			switch (State)
			{
				case Define.EnemyState.Chasing:
					return _chaseSpeed * ritualBonus;
				case Define.EnemyState.Searching:
					return _searchSpeed * ritualBonus;
				case Define.EnemyState.Patrol:
					return _patrolSpeed;
				default:
					return 0.0f;
			}
		}
	}

	protected override void UpdateIdle()
	{
		UpdatePatrol();
	}

	protected override void UpdatePatrol()
	{
		if (TryPetrify())
			return;

		if (TryDetectPlayer())
			return;

		if (_hasPatrolTarget == false
			|| Time.time >= _nextPatrolRetargetTime
			|| Vector2.Distance(transform.position, _patrolTarget) <= _arrivalRange
			|| IsStuck())
		{
			ChoosePatrolTarget();
		}

		Steer(_patrolTarget);
	}

	protected override void UpdateChasing()
	{
		if (TryPetrify())
			return;

		if (ResolvePlayer() == false)
		{
			State = Define.EnemyState.Searching;
			return;
		}

		Vector2 playerPosition = _player.transform.position;

		if (Vector2.Distance(transform.position, playerPosition) <= _catchRange)
		{
			_moveDir = Vector2.zero;
			State = Define.EnemyState.Caught;
			return;
		}

		if (CanSensePlayer())
		{
			_lastKnownPosition = playerPosition;
			Steer(_lastKnownPosition);
			return;
		}

		State = Define.EnemyState.Searching;
	}

	protected override void UpdateSearching()
	{
		if (TryPetrify())
			return;

		if (TryDetectPlayer())
			return;

		if (Vector2.Distance(transform.position, _lastKnownPosition) > _arrivalRange)
		{
			Steer(_lastKnownPosition);
			return;
		}

		_moveDir = Vector2.zero;

		if (SearchExpired)
		{
			ChoosePatrolTarget();
			State = Define.EnemyState.Patrol;
		}
	}

	protected override void UpdatePetrified()
	{
		_moveDir = Vector2.zero;

		if (IsLitByLamp())
			return;

		Managers.Sound.PlayAtPointOptional(ReleaseClip, transform.position, Define.Sound.Threat);
		_wasPetrified = false;
		State = _stateBeforePetrify == Define.EnemyState.Petrified
			? Define.EnemyState.Searching
			: _stateBeforePetrify;
	}

	protected override void UpdateCaught()
	{
		_moveDir = Vector2.zero;
		Managers.Game.GameOver(transform);
	}

	bool TryPetrify()
	{
		if (IsLitByLamp() == false)
			return false;

		if (_player != null)
			_lastKnownPosition = _player.transform.position;

		_stateBeforePetrify = State == Define.EnemyState.Chasing
			? Define.EnemyState.Chasing
			: Define.EnemyState.Searching;

		if (_wasPetrified == false)
		{
			_wasPetrified = true;
			Managers.Sound.PlayAtPointOptional(PetrifyClip, transform.position, Define.Sound.Threat);
		}

		_moveDir = Vector2.zero;
		State = Define.EnemyState.Petrified;
		return true;
	}

	bool IsLitByLamp()
	{
		if (ResolvePlayer() == false)
			return false;

		Lamp lamp = _player.Lamp;
		return lamp != null && lamp.IsOn && lamp.IsInLightCone(transform.position);
	}

	bool TryDetectPlayer()
	{
		if (CanSensePlayer() == false)
			return false;

		_lastKnownPosition = _player.transform.position;
		State = Define.EnemyState.Chasing;
		return true;
	}

	bool CanSensePlayer()
	{
		if (ResolvePlayer() == false)
			return false;

		float distance = Vector2.Distance(transform.position, _player.transform.position);

		if (_player.CurrentNoiseRadius > 0.0f
			&& distance <= _player.CurrentNoiseRadius * _hearingScale)
		{
			return true;
		}

		Lamp lamp = _player.Lamp;
		if (lamp == null || lamp.IsOn == false)
			return false;

		return distance <= _sightRange && HasLineOfSight(_player.transform.position);
	}

	bool HasLineOfSight(Vector3 target)
	{
		RaycastHit2D hit = Physics2D.Linecast(
			transform.position,
			target,
			1 << (int)Define.Layer.Block);

		return hit.collider == null;
	}

	void ChoosePatrolTarget()
	{
		_nextPatrolRetargetTime = Time.time + _patrolRetargetSeconds;
		_hasPatrolTarget = true;

		if (MapCoord.IsReady == false)
		{
			_patrolTarget = (Vector2)transform.position + Random.insideUnitCircle * 4.0f;
			return;
		}

		for (int attempt = 0; attempt < 12; attempt++)
		{
			Vector2 candidate = (Vector2)transform.position + Random.insideUnitCircle.normalized
				* Random.Range(3.0f, 8.0f);

			if (MapCoord.IsWalkable(candidate) == false)
				continue;

			_patrolTarget = candidate;
			return;
		}

		_patrolTarget = transform.position;
	}

	void Steer(Vector2 destination)
	{
		_moveDir = SteerTowards(destination);
		UpdateAnimatorDirection(_moveDir);
	}

	bool ResolvePlayer()
	{
		if (_player != null)
			return true;

		GameObject player = Managers.Game.GetPlayer();
		_player = player != null
			? player.GetComponent<PlayerController>()
			: Object.FindFirstObjectByType<PlayerController>();

		return _player != null;
	}
}
