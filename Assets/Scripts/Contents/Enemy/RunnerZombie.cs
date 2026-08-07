using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RunnerZombie : EnemyBase
{
	[SerializeField]
	float _moveSpeed = 12.35f;

	[SerializeField]
	float _routeTurnInterval = 22.0f;

	[SerializeField]
	float _silentReleaseDuration = 3.0f;

	[SerializeField, Range(0.1f, 1.0f)]
	float _hearingScale = 0.3f;

	Rigidbody2D _rigidbody;
	PlayerController _player;
	Vector2 _moveDir;
	float _nextRouteTurnTime;
	float _lastHeardTime = float.NegativeInfinity;
	float _nextAlertTime;
	bool _heardNoiseThisFrame;

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

		ChooseRouteDirection();
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
		_heardNoiseThisFrame = false;
		ListenForPlayerNoise();

		if (State == Define.EnemyState.Chasing)
			return;

		if (Time.time >= _nextRouteTurnTime || IsStuck())
			ChooseRouteDirection();
	}

	protected override void UpdateChasing()
	{
		_heardNoiseThisFrame = false;
		ListenForPlayerNoise();

		if (Time.time - _lastHeardTime < _silentReleaseDuration)
			return;

		ChooseRouteDirection();
		State = Define.EnemyState.Patrol;
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
			ChooseRouteDirection();
	}

	void ListenForPlayerNoise()
	{
		if (_player == null)
			_player = FindPlayer();

		if (_player == null || _player.CurrentNoiseRadius <= 0)
			return;

		Vector2 toNoise = _player.transform.position - transform.position;
		if (toNoise.magnitude > _player.CurrentNoiseRadius * _hearingScale)
			return;

		_moveDir = SteerTowards(_player.transform.position);
		_lastHeardTime = Time.time;
		_heardNoiseThisFrame = true;
		UpdateAnimatorDirection(_moveDir);
		State = Define.EnemyState.Chasing;
	}

	void UpdateAlert()
	{
		if (_heardNoiseThisFrame == false || _player == null)
			return;

		float distance = Vector2.Distance(transform.position, _player.transform.position);
		if (distance > 9.0f || Time.time < _nextAlertTime)
			return;

		float interval = 1.2f;
		int semitones = 0;

		if (distance <= 4.0f)
		{
			interval = 0.35f;
			semitones = 5;
		}
		else if (distance <= 6.0f)
		{
			interval = 0.6f;
			semitones = 2;
		}

		float pitch = Mathf.Pow(2.0f, semitones / 12.0f);
		Managers.Sound.PlayAtPointOptional(
			"runner_alert",
			"moster growl (4)",
			transform.position,
			Define.Sound.Threat,
			1.0f,
			pitch
		);
		_nextAlertTime = Time.time + interval;
	}

	void ChooseRouteDirection()
	{
		_moveDir = DeterministicHeading();
		_nextRouteTurnTime = Time.time + _routeTurnInterval;
		UpdateAnimatorDirection(_moveDir);
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
