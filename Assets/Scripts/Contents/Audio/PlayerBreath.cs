using UnityEngine;

/// 달리면 숨이 가빠지고 멈추면 가라앉는다. 남은 양을 재는 눈금이 아니라
/// 방금 무리했다는 신호다 — 스태미너를 없앤 뒤에도 이 감각은 남겨둔다.
public class PlayerBreath : MonoBehaviour
{
	[SerializeField]
	AudioClip _clip;

	[SerializeField]
	PlayerController _player;

	/// 이 초만큼 계속 달리면 숨소리가 최대가 된다.
	[SerializeField]
	float _windUpSeconds = 4.0f;

	[SerializeField]
	float _calmSeconds = 3.0f;

	[SerializeField]
	float _maxVolume = 0.85f;

	AudioSource _source;
	float _effort;
	float _volume;

	public float Volume { get { return _volume; } }

	/// 달린 시간이 쌓일수록 커지고, 멈추면 줄어든다.
	public static float NextEffort(float effort, bool running, float delta,
		float windUpSeconds, float calmSeconds)
	{
		float speed = running
			? (windUpSeconds > 0.0f ? delta / windUpSeconds : 1.0f)
			: -(calmSeconds > 0.0f ? delta / calmSeconds : 1.0f);

		return Mathf.Clamp01(effort + speed);
	}

	void Awake()
	{
		if (_player == null)
			_player = GetComponentInParent<PlayerController>();

		if (_clip == null)
			_clip = Managers.Resource.Load<AudioClip>("Audio/breath_tired");

		_source = Util.GetOrAddComponent<AudioSource>(gameObject);
		_source.clip = _clip;
		_source.loop = true;
		_source.playOnAwake = false;
		_source.spatialBlend = 0.0f;
		_source.volume = 0.0f;
	}

	void Update()
	{
		if (_source == null || _player == null || AudioTuning.IsReady(_source.clip) == false)
			return;

		_effort = NextEffort(_effort, _player.IsRunning, Time.deltaTime,
			_windUpSeconds, _calmSeconds);

		_volume = _effort;
		_source.volume = _volume * _maxVolume;

		if (_volume > 0.01f && _source.isPlaying == false)
			_source.Play();
		else if (_volume <= 0.01f && _source.isPlaying)
			_source.Stop();
	}
}
