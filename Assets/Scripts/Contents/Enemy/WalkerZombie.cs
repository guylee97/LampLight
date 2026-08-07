using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WalkerZombie : EnemyBase
{
	[SerializeField]
	float _moveSpeed = 2.5f;

	[SerializeField, Range(0.1f, 1.0f)]
	float _hearingScale = 0.3f;

	[SerializeField]
	float _catchRange = 0.4f;

	[SerializeField]
	float _destinationArrivalRange = 0.1f;

	Rigidbody2D _rigidbody;
	PlayerController _player;
	Vector2 _moveDir;
	Vector2 _lastHeardPosition;
	bool _hasLastHeardPosition;

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

		if (CanHearPlayer())
		{
			RememberPlayerNoise();
			State = Define.EnemyState.Chasing;
		}
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

			if (CanHearPlayer())
				RememberPlayerNoise();
		}

		if (_hasLastHeardPosition == false)
		{
			_moveDir = Vector2.zero;
			State = Define.EnemyState.Idle;
			return;
		}

		Vector2 toLastHeardPosition = _lastHeardPosition - (Vector2)transform.position;
		if (toLastHeardPosition.magnitude <= _destinationArrivalRange)
		{
			_moveDir = Vector2.zero;
			_hasLastHeardPosition = false;
			State = Define.EnemyState.Idle;
			return;
		}

		_moveDir = SteerTowards(_lastHeardPosition);
		UpdateAnimatorDirection(_moveDir);
	}

	protected override void UpdateCaught()
	{
		_moveDir = Vector2.zero;
		Managers.Game.GameOver(transform);
	}

	bool CanHearPlayer()
	{
		if (_player == null || _player.CurrentNoiseRadius <= 0)
			return false;

		float distance = Vector2.Distance(transform.position, _player.transform.position);
		return distance <= _player.CurrentNoiseRadius * _hearingScale;
	}

	void RememberPlayerNoise()
	{
		_lastHeardPosition = _player.transform.position;
		_hasLastHeardPosition = true;
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
