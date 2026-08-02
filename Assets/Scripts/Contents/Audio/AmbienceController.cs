using UnityEngine;

public class AmbienceController : MonoBehaviour
{
	[SerializeField]
	AudioClip _ambientClip;

	[SerializeField]
	AudioClip _heartbeatClip;

	[SerializeField]
	float _warningSeconds = 20.0f;

	[SerializeField]
	bool _ambientIsStinger = true;

	[SerializeField]
	float _stingerMinGap = 55.0f;

	[SerializeField]
	float _stingerMaxGap = 110.0f;

	float _nextStingerTime = -1.0f;

	[SerializeField]
	float _slowestBeat = 1.10f;

	[SerializeField]
	float _fastestBeat = 0.42f;

	AudioSource _ambient;
	AudioSource _heart;
	Lamp _lamp;
	double _nextBeatDsp;

	public static float BeatInterval(float remaining, float warning, float slowest, float fastest)
	{
		if (warning <= 0.0f)
			return slowest;

		float t = Mathf.Clamp01(remaining / warning);
		return Mathf.Lerp(fastest, slowest, t);
	}

	void Awake()
	{
		if (_ambientClip == null)
			_ambientClip = Managers.Resource.Load<AudioClip>("Sounds/ambient_temple");

		if (_heartbeatClip == null)
			_heartbeatClip = Managers.Resource.Load<AudioClip>("Sounds/lantern_low");

		_ambient = CreateSource("Ambient", _ambientClip, _ambientIsStinger == false);
		_heart = CreateSource("Heartbeat", _heartbeatClip, false);
	}

	AudioSource CreateSource(string childName, AudioClip clip, bool loop)
	{
		GameObject go = new GameObject(childName);
		go.transform.SetParent(transform, false);

		AudioSource source = go.AddComponent<AudioSource>();
		source.clip = clip;
		source.loop = loop;
		source.playOnAwake = false;
		source.spatialBlend = 0.0f;
		return source;
	}

	void Update()
	{
		UpdateAmbient();
		UpdateHeartbeat();
	}

	void UpdateAmbient()
	{
		if (_ambient == null || AudioTuning.IsReady(_ambient.clip) == false)
			return;

		_ambient.volume = 1.0f;

		if (Managers.Game.IsPlaying == false)
			return;

		if (_ambientIsStinger == false)
		{
			if (_ambient.isPlaying == false)
				_ambient.Play();

			return;
		}

		if (_nextStingerTime < 0.0f)
		{
			_nextStingerTime = Time.time + Random.Range(_stingerMinGap, _stingerMaxGap);
			return;
		}

		if (_ambient.isPlaying || Time.time < _nextStingerTime)
			return;

		_ambient.Play();
		_nextStingerTime = Time.time + Random.Range(_stingerMinGap, _stingerMaxGap);
	}

	void UpdateHeartbeat()
	{
		if (_heart == null || AudioTuning.IsReady(_heart.clip) == false)
			return;

		if (_lamp == null)
		{
			PlayerController player = FindFirstObjectByType<PlayerController>();
			_lamp = player != null ? player.Lamp : null;
		}

		if (_lamp == null || _lamp.IsOn == false || Managers.Game.IsPlaying == false)
		{
			_nextBeatDsp = 0.0;
			return;
		}

		float remaining = _lamp.RemainingDuration;
		if (remaining > _warningSeconds)
		{
			_nextBeatDsp = 0.0;
			return;
		}

		double now = AudioSettings.dspTime;
		double lookahead = AudioTuning.ScheduleLookaheadSeconds;

		if (_nextBeatDsp <= 0.0 || _nextBeatDsp < now)
			_nextBeatDsp = now + lookahead;

		if (now + lookahead < _nextBeatDsp)
			return;

		_heart.volume = 1.0f;
		_heart.PlayScheduled(_nextBeatDsp);
		_nextBeatDsp += BeatInterval(remaining, _warningSeconds, _slowestBeat, _fastestBeat);
	}
}
