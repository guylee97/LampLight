using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DefaultEnemy : BaseController, ILampReactive
{
	[SerializeField]
	float _moveSpeed = 2.5f;

	[SerializeField]
	float _catchRange = 0.4f;

	Rigidbody2D _rigidbody;
	PlayerController _player;
	Vector2 _moveDir;

	public override Define.State State
	{
		get { return _state; }
		set { _state = value; }
	}

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
			State = Define.State.Moving;
	}

	protected override void UpdateMoving()
	{
		if (_player == null)
			_player = FindPlayer();

		if (_player == null)
		{
			State = Define.State.Idle;
			return;
		}

		Vector2 toPlayer = _player.transform.position - transform.position;
		float distance = toPlayer.magnitude;

		if (distance <= _catchRange)
		{
			_moveDir = Vector2.zero;
			State = Define.State.Skill;
			return;
		}

		_moveDir = toPlayer.normalized;
	}

	protected override void UpdateSkill()
	{
		_moveDir = Vector2.zero;
		Debug.Log("Game Over: Enemy caught the player.");
	}

	void Move()
	{
		if (_moveDir.sqrMagnitude <= 0.01f)
			return;

		Vector2 movement = _moveDir * _moveSpeed * Time.fixedDeltaTime;
		_rigidbody.MovePosition(_rigidbody.position + movement);
	}

	PlayerController FindPlayer()
	{
		GameObject player = Managers.Game.GetPlayer();
		if (player != null)
			return player.GetComponent<PlayerController>();

		return Object.FindFirstObjectByType<PlayerController>();
	}

	public void OnLampEnter()
	{
		Debug.Log($"{name}: Lamp entered.");
	}

	public void OnLampStay()
	{
	}

	public void OnLampExit()
	{
		Debug.Log($"{name}: Lamp exited.");
	}
}
