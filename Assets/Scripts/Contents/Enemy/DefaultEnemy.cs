using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DefaultEnemy : BaseController, ILampReactive, IFacingSource
{
	[SerializeField]
	float _patrolSpeed = 1.4f;

	[SerializeField]
	float _chaseSpeed = 3.1f;

	[SerializeField]
	float _blindedSpeed = 2.2f;

	[SerializeField]
	float _catchRange = 0.45f;

	[SerializeField]
	float _sightRange = 9.0f;

	[SerializeField]
	float _sightAngle = 100.0f;

	[SerializeField]
	float _closeSenseRange = 1.6f;

	[SerializeField]
	LayerMask _sightObstacleMask;

	[SerializeField]
	float _mutedHearingScale = 0.45f;

	[SerializeField]
	float _repathInterval = 0.35f;

	[SerializeField]
	float _waypointTolerance = 0.22f;

	[SerializeField]
	float _patrolWaitTime = 1.1f;

	[SerializeField]
	int _patrolRadius = 14;

	[SerializeField]
	float _searchDuration = 6.0f;

	[SerializeField]
	float _blindDuration = 1.6f;

	[SerializeField]
	float _separationRadius = 0.85f;

	[SerializeField]
	float _separationStrength = 1.2f;

	[SerializeField]
	LayerMask _separationMask;

	[SerializeField]
	DirectionalSprite _directionalSprite;

	[SerializeField]
	Define.Direction8 _startDirection = Define.Direction8.S;

	[SerializeField]
	Define.Alert _alert = Define.Alert.Patrol;

	Rigidbody2D _rigidbody;
	PlayerController _player;
	Vector2 _moveDir;
	float _speed;
	Define.Direction8 _direction;

	readonly List<Vector2Int> _path = new List<Vector2Int>();
	int _pathIndex;
	Vector2Int _goalTile;
	float _nextRepathTime;
	float _stateTimer;
	Vector3 _lastKnownPosition;
	System.Random _rng;

	readonly Collider2D[] _neighbours = new Collider2D[8];
	ContactFilter2D _separationFilter;

	public Define.Alert Alert { get { return _alert; } }

	public Define.Direction8 Direction { get { return _direction; } }

	public Vector2 Facing { get { return DirectionUtil.ToVector(_direction); } }

	public override Define.State State
	{
		get { return _state; }
		set { _state = value; }
	}

	public override void Init()
	{
		WorldObjectType = Define.WorldObject.Enemy;
		_rigidbody = GetComponent<Rigidbody2D>();

		if (_directionalSprite == null)
			_directionalSprite = GetComponentInChildren<DirectionalSprite>();

		SetDirection(_startDirection);

		if (_rigidbody == null)
			_rigidbody = gameObject.AddComponent<Rigidbody2D>();

		_rigidbody.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody.gravityScale = 0;
		_rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
		_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

		if (GetComponent<Collider2D>() == null)
			gameObject.AddComponent<CircleCollider2D>();

		_separationFilter = new ContactFilter2D();
		_separationFilter.useLayerMask = true;
		_separationFilter.layerMask = _separationMask;
		_separationFilter.useTriggers = false;

		_rng = new System.Random(GetInstanceID());
		_lastKnownPosition = transform.position;
		EnterPatrol();
	}

	public void Relocate(Vector3 position)
	{
		transform.position = position;

		if (_rigidbody != null)
			_rigidbody.position = position;

		_path.Clear();
		_pathIndex = 0;
		_lastKnownPosition = position;
		_alert = Define.Alert.Patrol;
		_stateTimer = 0;
		_nextRepathTime = 0;
	}

	protected override void Update()
	{
		if (Managers.Game.IsPlaying == false)
		{
			_moveDir = Vector2.zero;
			return;
		}

		if (_player == null)
			_player = FindPlayer();

		Tick();
	}

	void FixedUpdate()
	{
		Move();
	}

	void Tick()
	{
		switch (_alert)
		{
			case Define.Alert.Patrol:
				TickPatrol();
				break;
			case Define.Alert.Suspicious:
				TickSuspicious();
				break;
			case Define.Alert.Chase:
				TickChase();
				break;
			case Define.Alert.Search:
				TickSearch();
				break;
			case Define.Alert.Blinded:
				TickBlinded();
				break;
			case Define.Alert.Caught:
				_moveDir = Vector2.zero;
				break;
		}
	}

	void TickPatrol()
	{
		if (DetectPlayer())
			return;

		if (_path.Count == 0)
		{
			_stateTimer -= Time.deltaTime;
			_moveDir = Vector2.zero;
			State = Define.State.Idle;

			if (_stateTimer <= 0)
				PickPatrolGoal();

			return;
		}

		_speed = _patrolSpeed;
		FollowPath();

		if (_path.Count == 0)
			_stateTimer = _patrolWaitTime;
	}

	void TickSuspicious()
	{
		if (DetectPlayer())
			return;

		_speed = _chaseSpeed * 0.8f;
		RepathTo(_lastKnownPosition);
		FollowPath();

		if (_path.Count == 0 || Vector2.Distance(transform.position, _lastKnownPosition) <= _waypointTolerance)
			EnterSearch();
	}

	void TickChase()
	{
		if (_player == null)
		{
			EnterSearch();
			return;
		}

		bool visible = CanSee(_player.transform.position);
		if (visible)
			_lastKnownPosition = _player.transform.position;

		float distance = Vector2.Distance(transform.position, _player.transform.position);
		if (distance <= _catchRange)
		{
			_alert = Define.Alert.Caught;
			_moveDir = Vector2.zero;
			State = Define.State.Skill;
			Managers.Game.ReportPlayerCaught();
			return;
		}

		if (visible == false && CanHear(distance) == false)
		{
			EnterSearch();
			return;
		}

		_speed = _chaseSpeed;
		RepathTo(_lastKnownPosition);
		FollowPath();
	}

	void TickSearch()
	{
		if (DetectPlayer())
			return;

		_stateTimer -= Time.deltaTime;
		if (_stateTimer <= 0)
		{
			EnterPatrol();
			return;
		}

		_speed = _patrolSpeed * 1.3f;

		if (_path.Count == 0)
			SetGoal(RandomWalkableNear(MapCoord.WorldToTile(_lastKnownPosition), 5));

		FollowPath();
	}

	void TickBlinded()
	{
		_stateTimer -= Time.deltaTime;
		_speed = _blindedSpeed;
		_path.Clear();

		if (_player != null)
		{
			Vector2 away = transform.position - _player.transform.position;
			_moveDir = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.zero;
			SetDirection(DirectionUtil.FromVector(-_moveDir, _direction));
			_lastKnownPosition = _player.transform.position;
		}

		State = Define.State.Moving;

		if (_stateTimer <= 0)
			EnterSuspicious(_lastKnownPosition);
	}

	bool DetectPlayer()
	{
		if (_player == null)
			return false;

		if (CanSee(_player.transform.position))
		{
			_lastKnownPosition = _player.transform.position;
			EnterChase();
			return true;
		}

		float distance = Vector2.Distance(transform.position, _player.transform.position);
		if (CanHear(distance))
		{
			EnterSuspicious(_player.transform.position);
			return true;
		}

		return false;
	}

	public bool CanSee(Vector3 target)
	{
		Vector2 toTarget = target - transform.position;
		float sqrDistance = toTarget.sqrMagnitude;

		float range = _sightRange * (_player != null ? _player.VisibilityScale : 1.0f);
		if (sqrDistance > range * range)
			return false;

		if (sqrDistance > _closeSenseRange * _closeSenseRange
			&& Vector2.Angle(Facing, toTarget.normalized) > _sightAngle * 0.5f)
			return false;

		return IsBlocked(target) == false;
	}

	bool CanHear(float distance)
	{
		if (_player == null)
			return false;

		float radius = _player.CurrentNoiseRadius;
		if (radius <= 0)
			return false;

		if (IsBlocked(_player.transform.position))
			radius *= _mutedHearingScale;

		return distance <= radius;
	}

	bool IsBlocked(Vector3 target)
	{
		if (_sightObstacleMask.value == 0)
			return false;

		return Physics2D.Linecast(transform.position, target, _sightObstacleMask).collider != null;
	}

	void EnterPatrol()
	{
		_alert = Define.Alert.Patrol;
		_path.Clear();
		PickPatrolGoal();
	}

	void EnterSuspicious(Vector3 position)
	{
		_alert = Define.Alert.Suspicious;
		_lastKnownPosition = position;
		_nextRepathTime = 0;
		_path.Clear();
	}

	void EnterChase()
	{
		if (_alert != Define.Alert.Chase)
			_nextRepathTime = 0;

		_alert = Define.Alert.Chase;
	}

	void EnterSearch()
	{
		_alert = Define.Alert.Search;
		_stateTimer = _searchDuration;
		_path.Clear();
	}

	void PickPatrolGoal()
	{
		Vector2Int here = MapCoord.WorldToTile(transform.position);
		SetGoal(RandomWalkableNear(here, _patrolRadius));
		_stateTimer = _patrolWaitTime;
	}

	Vector2Int RandomWalkableNear(Vector2Int origin, int radius)
	{
		for (int attempt = 0; attempt < 24; attempt++)
		{
			int col = origin.x + _rng.Next(-radius, radius + 1);
			int row = origin.y + _rng.Next(-radius, radius + 1);

			if (MapCoord.IsWalkable(col, row))
				return new Vector2Int(col, row);
		}

		return origin;
	}

	void SetGoal(Vector2Int tile)
	{
		_goalTile = tile;
		_pathIndex = 0;

		Vector2Int here = MapCoord.WorldToTile(transform.position);
		if (MapPathfinder.TryFindPath(here, tile, _path) == false)
			_path.Clear();
		else if (_path.Count > 1 && _path[0] == here)
			_pathIndex = 1;
	}

	void RepathTo(Vector3 worldTarget)
	{
		Vector2Int tile = MapCoord.WorldToTile(worldTarget);

		if (Time.time < _nextRepathTime && _path.Count > 0 && tile == _goalTile)
			return;

		_nextRepathTime = Time.time + _repathInterval;
		SetGoal(tile);
	}

	void FollowPath()
	{
		if (_pathIndex >= _path.Count)
		{
			ArriveAtPathEnd();
			return;
		}

		Vector3 waypoint = MapCoord.TileToWorld(_path[_pathIndex].x, _path[_pathIndex].y);
		Vector2 toWaypoint = waypoint - transform.position;

		if (toWaypoint.magnitude <= _waypointTolerance)
		{
			_pathIndex++;
			if (_pathIndex >= _path.Count)
			{
				ArriveAtPathEnd();
				return;
			}

			waypoint = MapCoord.TileToWorld(_path[_pathIndex].x, _path[_pathIndex].y);
			toWaypoint = waypoint - transform.position;
		}

		_moveDir = (toWaypoint.normalized + Separation() * _separationStrength).normalized;
		SetDirection(DirectionUtil.FromVector(_moveDir, _direction));
		State = Define.State.Moving;
	}

	void ArriveAtPathEnd()
	{
		_path.Clear();
		_moveDir = Vector2.zero;
		State = Define.State.Idle;
	}

	Vector2 Separation()
	{
		if (_separationMask.value == 0)
			return Vector2.zero;

		int count = Physics2D.OverlapCircle(transform.position, _separationRadius, _separationFilter, _neighbours);
		Vector2 push = Vector2.zero;

		for (int i = 0; i < count; i++)
		{
			Collider2D other = _neighbours[i];
			if (other == null || other.transform.IsChildOf(transform))
				continue;

			Vector2 away = transform.position - other.transform.position;
			float distance = away.magnitude;
			if (distance <= 0.0001f)
				continue;

			push += away / distance * (1.0f - Mathf.Clamp01(distance / _separationRadius));
		}

		return push;
	}

	public void SetDirection(Define.Direction8 direction)
	{
		_direction = direction;

		if (_directionalSprite != null)
			_directionalSprite.Apply(direction);
	}

	void Move()
	{
		if (_rigidbody == null || _moveDir.sqrMagnitude <= 0.01f || _speed <= 0)
			return;

		Vector2 movement = _moveDir * _speed * Time.fixedDeltaTime;
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
		Blind();
	}

	public void OnLampStay()
	{
		Blind();
	}

	public void OnLampExit()
	{
	}

	void Blind()
	{
		if (_alert == Define.Alert.Caught)
			return;

		_alert = Define.Alert.Blinded;
		_stateTimer = _blindDuration;
	}
}
