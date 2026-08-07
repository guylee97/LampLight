using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WandererZombie : EnemyBase
{
	[SerializeField]
	float _patrolSpeed = 2.0f;

	[SerializeField]
	float _chaseSpeed = 3.5f;

	[SerializeField, Range(0.1f, 1.0f)]
	float _hearingScale = 0.3f;

	[SerializeField]
	float _catchRange = 0.4f;

	[SerializeField]
	float _minTurnInterval = 1.5f;

	[SerializeField]
	float _maxTurnInterval = 4.0f;

	Rigidbody2D _rigidbody;
	PlayerController _player;
	Vector2 _moveDir;
	float _moveSpeed;
	float _nextTurnTime;

	public override void Init()
	{
		WorldObjectType = Define.WorldObject.Enemy;
		_rigidbody = GetComponent<Rigidbody2D>();
		_rigidbody.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody.gravityScale = 0;
		_rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
		_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

		if (GetComponent<Collider2D>() == null)
			gameObject.AddComponent<CircleCollider2D>();

		_moveSpeed = _patrolSpeed;
		ChoosePatrolDirection();
		State = Define.EnemyState.Patrol;
	}

	void FixedUpdate()
	{
		if (_moveDir.sqrMagnitude <= 0.01f)
			return;

		Vector2 movement = _moveDir * _moveSpeed * SpeedMultiplier * Time.fixedDeltaTime;
		_rigidbody.MovePosition(_rigidbody.position + movement);
	}

	protected override void UpdatePatrol()
	{
		if (_player == null)
			_player = FindPlayer();

		if (CanHearPlayer())
		{
			_moveSpeed = _chaseSpeed;
			State = Define.EnemyState.Chasing;
			return;
		}

		if (Time.time >= _nextTurnTime || IsStuck())
			ChoosePatrolDirection();
	}

	protected override void UpdateChasing()
	{
		if (_player == null)
			_player = FindPlayer();

		if (_player != null)
		{
			Vector2 toPlayer = _player.transform.position - transform.position;
			if (toPlayer.magnitude <= _catchRange)
			{
				_moveDir = Vector2.zero;
				State = Define.EnemyState.Caught;
				return;
			}
		}

		if (!CanHearPlayer())
		{
			ChoosePatrolDirection();
			State = Define.EnemyState.Patrol;
			return;
		}

		_moveDir = SteerTowards(_player.transform.position);
		UpdateAnimatorDirection(_moveDir);
	}

	protected override void UpdateCaught()
	{
		_moveDir = Vector2.zero;
		Managers.Game.GameOver(transform);
	}

	protected override void OnCollisionEnter2D(Collision2D collision)
	{
		base.OnCollisionEnter2D(collision);

		if (State == Define.EnemyState.Patrol)
			ChoosePatrolDirection();
	}

	void ChoosePatrolDirection()
	{
		_moveDir = DeterministicHeading();
		UpdateAnimatorDirection(_moveDir);
		_moveSpeed = _patrolSpeed;
		_nextTurnTime = Time.time + (_minTurnInterval + _maxTurnInterval) * 0.5f;
	}

	bool CanHearPlayer()
	{
		if (_player == null || _player.CurrentNoiseRadius <= 0)
			return false;

		float distance = Vector2.Distance(transform.position, _player.transform.position);
		return distance <= _player.CurrentNoiseRadius * _hearingScale;
	}

	PlayerController FindPlayer()
	{
		GameObject player = Managers.Game.GetPlayer();
		if (player != null)
			return player.GetComponent<PlayerController>();

		return Object.FindFirstObjectByType<PlayerController>();
	}


	int _headingStep;

	Vector2 DeterministicHeading()
	{
		_headingStep++;
		float angle = (_headingStep * 137.5f + Mathf.Abs(GetInstanceID()) % 360) * Mathf.Deg2Rad;
		return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
	}
}
