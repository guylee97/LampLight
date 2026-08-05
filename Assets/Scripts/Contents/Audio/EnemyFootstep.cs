using UnityEngine;

[RequireComponent(typeof(OcclusionSource))]
public class EnemyFootstep : MonoBehaviour
{
	[SerializeField]
	OcclusionSource _source;

	[SerializeField]
	float _interval = 0.9f;

	[SerializeField, Range(0.0f, 0.5f)]
	float _jitter;

	[SerializeField]
	float _audibleRadius = 8.0f;

	EnemyBase _enemy;
	Transform _listener;
	double _nextDsp;
	float _pendingInterval;

	public float Interval { get { return _interval; } }
	public float Jitter { get { return _jitter; } }

	public static float NextInterval(float baseInterval, float jitter, float roll)
	{
		if (jitter <= 0.0f)
			return baseInterval;

		return baseInterval * (1.0f + Mathf.Clamp(roll, -1.0f, 1.0f) * jitter);
	}

	void Awake()
	{
		if (_source == null)
			_source = GetComponent<OcclusionSource>();

		_enemy = GetComponent<EnemyBase>();
		_pendingInterval = _interval;
	}

	void Update()
	{
		if (_source == null)
			return;

		if (_listener == null)
		{
			PlayerController player = FindFirstObjectByType<PlayerController>();
			if (player == null)
				return;

			_listener = player.transform;
			_source.SetListener(_listener);
		}

		if (IsMoving() == false)
		{
			_nextDsp = 0.0;
			return;
		}

		float distance = Vector2.Distance(transform.position, _listener.position);
		if (distance > _audibleRadius * AudioTuning.HearingScale)
		{
			_nextDsp = 0.0;
			return;
		}

		double now = AudioSettings.dspTime;
		double lookahead = AudioTuning.ScheduleLookaheadSeconds;

		if (_nextDsp <= 0.0 || _nextDsp < now)
			_nextDsp = now + lookahead;

		if (now + lookahead < _nextDsp)
			return;

		_source.PlayScheduled(_nextDsp);
		_nextDsp += _pendingInterval;
		_pendingInterval = NextInterval(_interval, _jitter, Random.Range(-1.0f, 1.0f));

		// 소리 방향 HUD 기능 제거.
		// SoundRing.Emit(transform.position - _listener.position, SoundRing.ThreatColor, distance);
	}

	bool IsMoving()
	{
		if (_enemy == null)
			return true;

		return _enemy.State == Define.EnemyState.Patrol || _enemy.State == Define.EnemyState.Chasing;
	}
}
