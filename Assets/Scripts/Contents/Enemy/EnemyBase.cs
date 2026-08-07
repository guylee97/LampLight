using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, ILampReactive
{
	const float PathRefreshInterval = 0.4f;
	const float WaypointArrivalRange = 0.2f;
	const float StuckCheckInterval = 0.5f;
	const float StuckMoveThreshold = 0.05f;

	[SerializeField]
	protected Define.EnemyState _state;

	[SerializeField]
	AudioClip _chaseSound;

	[SerializeField]
	float _chaseSoundInterval = 2.0f;

	[SerializeField, Range(0.0f, 1.0f)]
	float _chaseSoundVolume = 1.0f;

	[SerializeField]
	float _audibleDistance = 4.0f;

	[SerializeField]
	float _searchSeconds = 3.0f;

	Animator _animator;
	DirectionalSpriteAnimator _directional;
	Coroutine _slowCoroutine;
	float _speedMultiplier = 1.0f;
	float _nextChaseSoundTime;
	float _searchUntil;
	bool _countedAsChasing;

	readonly List<Vector2Int> _path = new List<Vector2Int>();
	int _pathIndex;
	Vector2Int _pathGoal = new Vector2Int(int.MinValue, int.MinValue);
	float _nextPathTime;
	Vector2 _lastStuckSample;
	float _nextStuckCheckTime;

	public Define.WorldObject WorldObjectType { get; protected set; } = Define.WorldObject.Enemy;
	protected float SpeedMultiplier { get { return _speedMultiplier; } }
	protected float SearchSeconds { get { return _searchSeconds; } }
	protected bool SearchExpired { get { return Time.time >= _searchUntil; } }

	public Define.Awareness Awareness
	{
		get
		{
			switch (State)
			{
				case Define.EnemyState.Chasing:
				case Define.EnemyState.Caught:
					return Define.Awareness.Alerted;
				case Define.EnemyState.Searching:
					return Define.Awareness.Suspicious;
				default:
					return Define.Awareness.Unaware;
			}
		}
	}

	public Define.EnemyState State
	{
		get { return _state; }
		protected set
		{
			if (_state == value)
				return;

			_state = value;
			SyncChaseMix();
			UpdateStateSound();
			UpdateAnimatorState();

			if (_state == Define.EnemyState.Searching)
				_searchUntil = Time.time + _searchSeconds;
		}
	}

	void SyncChaseMix()
	{
		bool chasing = _state == Define.EnemyState.Chasing;

		if (chasing == _countedAsChasing)
			return;

		_countedAsChasing = chasing;

		if (chasing)
			HorrorMix.EnterChase();
		else
			HorrorMix.ExitChase();
	}

	protected virtual void OnDestroy()
	{
		if (_countedAsChasing == false)
			return;

		_countedAsChasing = false;
		HorrorMix.ExitChase();
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
		if (Managers.Game.IsPlaying == false && State != Define.EnemyState.Caught)
			return;

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
			case Define.EnemyState.Searching:
				UpdateSearching();
				break;
			case Define.EnemyState.Caught:
				UpdateCaught();
				break;
			case Define.EnemyState.Die:
				UpdateDie();
				break;
		}

		UpdateChaseSound();
	}

	public abstract void Init();

	protected virtual void UpdateIdle() { }
	protected virtual void UpdatePatrol() { }
	protected virtual void UpdateChasing() { }
	protected virtual void UpdateSearching() { }
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

	protected Vector2 SteerTowards(Vector3 destination)
	{
		Vector2 toDestination = (Vector2)destination - (Vector2)transform.position;

		if (MapCoord.IsReady == false)
			return toDestination.normalized;

		Vector2Int goal = MapCoord.WorldToTile(destination);
		Vector2Int self = MapCoord.WorldToTile(transform.position);

		if (self == goal)
			return toDestination.normalized;

		if (goal != _pathGoal || Time.time >= _nextPathTime)
			Repath(self, goal);

		while (_pathIndex < _path.Count && ReachedWaypoint(_path[_pathIndex]))
			_pathIndex++;

		if (_pathIndex >= _path.Count)
			return toDestination.normalized;

		Vector3 waypoint = MapCoord.TileToWorld(_path[_pathIndex].x, _path[_pathIndex].y);
		return ((Vector2)waypoint - (Vector2)transform.position).normalized;
	}

	void Repath(Vector2Int self, Vector2Int goal)
	{
		_pathGoal = goal;
		_nextPathTime = Time.time + PathRefreshInterval;

		_pathIndex = MapPathfinder.TryFindPath(self, goal, _path) ? 1 : 0;
	}

	bool ReachedWaypoint(Vector2Int tile)
	{
		Vector3 waypoint = MapCoord.TileToWorld(tile.x, tile.y);
		Vector2 offset = (Vector2)waypoint - (Vector2)transform.position;
		return offset.sqrMagnitude <= WaypointArrivalRange * WaypointArrivalRange;
	}

	protected bool IsStuck()
	{
		if (Time.time < _nextStuckCheckTime)
			return false;

		_nextStuckCheckTime = Time.time + StuckCheckInterval;

		Vector2 now = transform.position;
		float moved = (now - _lastStuckSample).magnitude;
		_lastStuckSample = now;

		return moved < StuckMoveThreshold;
	}

	protected void UpdateAnimatorDirection(Vector2 direction)
	{
		if (direction.sqrMagnitude <= 0.01f)
			return;

		if (_directional == null)
			_directional = GetComponent<DirectionalSpriteAnimator>();

		if (_directional != null)
			_directional.SetHeading(direction);

		if (_animator == null)
			_animator = GetComponent<Animator>();

		if (_animator == null)
			return;

		_animator.SetFloat("MoveX", direction.x);
		_animator.SetFloat("MoveY", direction.y);
	}

	void UpdateAnimatorState()
	{
		UpdateDirectionalState();

		if (_animator == null)
			_animator = GetComponent<Animator>();

		if (_animator == null)
			return;

		_animator.speed = 1.0f;

		if (State == Define.EnemyState.Caught)
		{
			_animator.SetBool("IsMoving", true);
			_animator.SetBool("IsChasing", true);
			return;
		}

		bool isChasing = State == Define.EnemyState.Chasing;
		bool isMoving = State == Define.EnemyState.Patrol
			|| State == Define.EnemyState.Searching
			|| isChasing;
		_animator.SetBool("IsMoving", isMoving);
		_animator.SetBool("IsChasing", isChasing);
	}

	void UpdateDirectionalState()
	{
		if (_directional == null)
			_directional = GetComponent<DirectionalSpriteAnimator>();

		if (_directional == null)
			return;

		switch (State)
		{
			case Define.EnemyState.Chasing:
			case Define.EnemyState.Caught:
				_directional.SetState(DirectionalSpriteAnimator.StateWalk);
				_directional.SetIntensity(2.1f);
				_directional.SetStutter(0.7f, 2.0f);
				break;
			case Define.EnemyState.Searching:
				_directional.SetState(DirectionalSpriteAnimator.StateWalk);
				_directional.SetIntensity(1.3f);
				_directional.SetStutter(0.55f, 1.0f);
				break;
			case Define.EnemyState.Patrol:
				_directional.SetState(DirectionalSpriteAnimator.StateWalk);
				_directional.SetIntensity(1.0f);
				_directional.SetStutter(0.45f, 1.0f);
				break;
			default:
				_directional.SetState(DirectionalSpriteAnimator.StateIdle);
				_directional.SetIntensity(1.0f);
				_directional.SetStutter(0.0f, 0.0f);
				break;
		}
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
		if (State != Define.EnemyState.Chasing)
			return;

		PlayChaseSound();
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
