using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MaskYokai : EnemyBase
{
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

	[SerializeField]
	float _ritualNoticeRange = 1.6f;

	public static System.Action OnLostInDark;

	YokaiSpec _spec = YokaiTable.ForLevel(LevelTable.MinLevel);
	float _sensesResumeTime;
	Rigidbody2D _rigidbody;
	CircleCollider2D _body;
	PlayerController _player;
	Vector2 _moveDir;
	Vector2 _lastKnownPosition;
	Vector2 _patrolTarget;
	float _nextPatrolRetargetTime;
	bool _hasPatrolTarget;
	List<Vector2> _route;
	int _routeIndex;

	public YokaiSpec Spec { get { return _spec; } }

	public void UseSpec(YokaiSpec spec)
	{
		if (spec == null)
			return;

		_spec = spec;
		_patrolSpeed = spec.PatrolSpeed;
		_chaseSpeed = spec.ChaseSpeed;
		_searchSpeed = spec.SearchSpeed;
		_sightRange = spec.SightRange;
		_hearingScale = spec.HearingScale;
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

		_body = GetComponent<CircleCollider2D>();
		if (_body == null && GetComponent<Collider2D>() == null)
			_body = gameObject.AddComponent<CircleCollider2D>();

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
		if (TryDetectPlayer())
			return;

		if (DrawnToRitual())
		{
			State = Define.EnemyState.Searching;
			return;
		}

		if (_hasPatrolTarget == false
			|| Time.time >= _nextPatrolRetargetTime
			|| Vector2.Distance(transform.position, _patrolTarget) <= _arrivalRange)
		{
			ChoosePatrolTarget();
		}

		Steer(_patrolTarget);
	}

	protected override void UpdateChasing()
	{
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

		if (_player.Lamp != null && _player.Lamp.IsOn == false && OnLostInDark != null)
			OnLostInDark.Invoke();

		State = Define.EnemyState.Searching;
	}

	protected override void UpdateSearching()
	{
		if (TryDetectPlayer())
			return;

		if (DrawnToRitual())
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

	protected override void UpdateCaught()
	{
		_moveDir = Vector2.zero;
		Managers.Game.GameOver(transform);
	}

	bool TryDetectPlayer()
	{
		if (CanSensePlayer() == false)
			return false;

		_lastKnownPosition = _player.transform.position;
		State = Define.EnemyState.Chasing;
		return true;
	}

	bool DrawnToRitual()
	{
		Altar channeling = Altar.Channeling;
		if (channeling == null)
			return false;

		_hasPatrolTarget = false;
		_lastKnownPosition = channeling.Position;

		if (ResolvePlayer()
			&& Vector2.Distance(transform.position, _player.transform.position) <= _ritualNoticeRange)
		{
			State = Define.EnemyState.Chasing;
			return true;
		}

		Steer(_lastKnownPosition);
		return true;
	}

	public void HoldSensesFor(float seconds)
	{
		_sensesResumeTime = Time.time + Mathf.Max(0.0f, seconds);
	}

	bool CanSensePlayer()
	{
		if (Time.time < _sensesResumeTime)
			return false;

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

		MapData map = MapCoord.IsReady ? Managers.Data.Map : null;

		if (map == null || map.rooms == null || map.rooms.Length == 0)
		{
			_patrolTarget = transform.position;
			return;
		}

		if (_route == null)
			BuildRoute(map);

		if (_route.Count == 0)
		{
			_patrolTarget = transform.position;
			return;
		}

		_routeIndex = (_routeIndex + 1) % _route.Count;
		_patrolTarget = _route[_routeIndex];
	}

	void BuildRoute(MapData map)
	{
		_route = new List<Vector2>();

		foreach (MapRoom room in map.rooms)
		{
			Vector3 center = MapCoord.TileToWorld(
				room.col + room.width / 2,
				room.row + room.height / 2);

			_route.Add(center);
		}

		Vector2 here = transform.position;
		_route.Sort((a, b) => (a - here).sqrMagnitude.CompareTo((b - here).sqrMagnitude));
		_routeIndex = -1;
	}

	void Steer(Vector2 destination)
	{
		_moveDir = SteerTowards(destination);

		if (IsStuck())
			_moveDir = Unwedge(destination);

		UpdateAnimatorDirection(_moveDir);
	}

	Vector2 Unwedge(Vector2 destination)
	{
		Vector2 self = transform.position;
		Vector2 wanted = (destination - self).normalized;
		float radius = _body != null ? _body.radius : 0.45f;

		for (int i = 1; i <= 6; i++)
		{
			float sweep = 30.0f * i;

			foreach (float sign in StuckSweepSigns)
			{
				Vector2 candidate = Rotate(wanted, sweep * sign);
				if (IsClear(self, candidate, radius))
					return candidate;
			}
		}

		return -wanted;
	}

	static readonly float[] StuckSweepSigns = { 1.0f, -1.0f };

	bool IsClear(Vector2 origin, Vector2 direction, float radius)
	{
		RaycastHit2D hit = Physics2D.CircleCast(
			origin,
			radius * 0.95f,
			direction,
			1.1f,
			1 << (int)Define.Layer.Block);

		return hit.collider == null;
	}

	static Vector2 Rotate(Vector2 value, float degrees)
	{
		float rad = degrees * Mathf.Deg2Rad;
		float cos = Mathf.Cos(rad);
		float sin = Mathf.Sin(rad);
		return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
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
