using System;
using UnityEngine;

[RequireComponent(typeof(OcclusionSource))]
public class PingScheduler : MonoBehaviour
{
	[SerializeField]
	OcclusionSource _source;

	[SerializeField]
	Transform _listener;

	[SerializeField]
	float _radiusTiles = 12.0f;

	[SerializeField]
	float _semitoneOffset;

	[SerializeField]
	bool _active = true;

	double _nextDspTime;

	public Action<Vector3> OnPinged;

	public bool Active
	{
		get { return _active; }
		set
		{
			_active = value;
			if (_active == false)
				_nextDspTime = 0.0;
		}
	}

	public float RadiusTiles
	{
		get { return _radiusTiles; }
		set
		{
			_radiusTiles = value;
			if (_source != null)
				_source.RadiusTiles = value;
		}
	}

	public float DistanceTiles
	{
		get
		{
			if (_listener == null)
				return float.MaxValue;

			Vector2 a = transform.position;
			Vector2 b = _listener.position;
			return Vector2.Distance(a, b);
		}
	}

	public float CurrentPeriod { get { return AudioTuning.PingPeriod(DistanceTiles); } }

	void Awake()
	{
		if (_source == null)
			_source = GetComponent<OcclusionSource>();
	}

	void Start()
	{
		if (_listener == null)
		{
			PlayerController player = FindFirstObjectByType<PlayerController>();
			if (player != null)
				SetListener(player.transform);
		}

		if (_source != null)
			_source.RadiusTiles = _radiusTiles;
	}

	public void SetListener(Transform listener)
	{
		_listener = listener;

		if (_source != null)
			_source.SetListener(listener);
	}

	void Update()
	{
		if (_active == false || _listener == null || _source == null)
		{
			_nextDspTime = 0.0;
			return;
		}

		float distance = DistanceTiles;
		if (distance >= _radiusTiles * AudioTuning.HearingScale)
		{
			_nextDspTime = 0.0;
			return;
		}

		double now = AudioSettings.dspTime;
		double lookahead = AudioTuning.ScheduleLookaheadSeconds;

		if (_nextDspTime <= 0.0 || _nextDspTime < now)
			_nextDspTime = now + lookahead;

		if (now + lookahead < _nextDspTime)
			return;

		_source.PlayScheduled(_nextDspTime, Pitch());
		_nextDspTime += AudioTuning.PingPeriod(distance);

		Vector3 offset = transform.position - _listener.position;
		// 소리 방향 HUD 기능 제거.
		// SoundRing.Emit(offset, SoundRing.GuideColor, distance);

		if (OnPinged != null)
			OnPinged.Invoke(offset);
	}

	float Pitch()
	{
		if (Mathf.Approximately(_semitoneOffset, 0.0f))
			return 1.0f;

		return Mathf.Pow(2.0f, _semitoneOffset / 12.0f);
	}
}
