using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DefaultZombie : EnemyBase
{
	const float LampStunDuration = 0.5f;

	[SerializeField]
	float _moveSpeed = 2.5f;

	[SerializeField]
	float _catchRange = 0.4f;

	Rigidbody2D _rigidbody;
	PlayerController _player;
	Vector2 _moveDir;

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
	}

	void FixedUpdate()
	{
		Move();
	}

	protected override void UpdateIdle()
	{
		_player = FindPlayer();
		if (_player == null)
			return;

		float distance = Vector2.Distance(transform.position, _player.transform.position);
		if (_player.CurrentNoiseRadius > 0 && distance <= _player.CurrentNoiseRadius)
			State = Define.EnemyState.Chasing;
	}

	protected override void UpdateChasing()
	{
		if (_player == null)
			_player = FindPlayer();

		if (_player == null)
		{
			_moveDir = Vector2.zero;
			State = Define.EnemyState.Idle;
			return;
		}

		Vector2 toPlayer = _player.transform.position - transform.position;
		float distance = toPlayer.magnitude;

		if (distance <= _catchRange)
		{
			_moveDir = Vector2.zero;
			State = Define.EnemyState.Caught;
			return;
		}

		if (_player.CurrentNoiseRadius <= 0 || distance > _player.CurrentNoiseRadius)
		{
			_moveDir = Vector2.zero;
			State = Define.EnemyState.Idle;
			return;
		}

		_moveDir = toPlayer.normalized;
		UpdateAnimatorDirection(_moveDir);
	}

	protected override void UpdateCaught()
	{
		_moveDir = Vector2.zero;
		Managers.Game.GameOver();
	}

	public override void OnLampEnter()
	{
		base.OnLampEnter();
		ApplyStun(LampStunDuration);
	}

	protected override void OnStunStarted()
	{
		_moveDir = Vector2.zero;
	}

	protected override void OnStunFinished()
	{
		State = Define.EnemyState.Idle;
	}

	void Move()
	{
		if (_moveDir.sqrMagnitude <= 0.01f)
			return;

		Vector2 movement = _moveDir * _moveSpeed * SpeedMultiplier * Time.fixedDeltaTime;
		_rigidbody.MovePosition(_rigidbody.position + movement);
	}

	PlayerController FindPlayer()
	{
		GameObject player = Managers.Game.GetPlayer();
		if (player != null)
			return player.GetComponent<PlayerController>();

		return Object.FindFirstObjectByType<PlayerController>();
	}

}
