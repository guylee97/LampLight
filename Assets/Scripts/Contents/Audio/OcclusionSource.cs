using UnityEngine;

public class OcclusionSource : MonoBehaviour
{
	[SerializeField]
	AudioClip _clearClip;

	[SerializeField]
	AudioClip _muffledClip;

	[SerializeField]
	Define.Sound _bus = Define.Sound.Guide;

	[SerializeField]
	float _radiusTiles = 12.0f;

	[SerializeField]
	Transform _listener;

	AudioSource _clear;
	AudioSource _muffled;
	float _nextOcclusionCheck;
	int _wallCount;

	public float RadiusTiles
	{
		get { return _radiusTiles; }
		set
		{
			_radiusTiles = value;
			ApplySpatial();
		}
	}

	public int WallCount { get { return _wallCount; } }

	public void Configure(AudioClip clearClip, AudioClip muffledClip, Define.Sound bus, float radiusTiles)
	{
		_clearClip = clearClip;
		_muffledClip = muffledClip;
		_bus = bus;
		_radiusTiles = radiusTiles;

		EnsureSources();
		ApplySpatial();
	}

	public void SetListener(Transform listener)
	{
		_listener = listener;
	}

	void Awake()
	{
		EnsureSources();
		ApplySpatial();
	}

	void EnsureSources()
	{
		if (_clear == null)
			_clear = CreateChild("Clear");

		if (_muffled == null)
			_muffled = CreateChild("Muffled");

		_clear.clip = _clearClip;
		_muffled.clip = _muffledClip != null ? _muffledClip : _clearClip;
	}

	AudioSource CreateChild(string childName)
	{
		Transform found = transform.Find(childName);
		GameObject go = found != null ? found.gameObject : new GameObject(childName);
		go.transform.SetParent(transform, false);

		AudioSource source = Util.GetOrAddComponent<AudioSource>(go);
		source.playOnAwake = false;
		return source;
	}

	void ApplySpatial()
	{
		if (_clear != null)
			AudioTuning.ApplySpatial(_clear, _radiusTiles);

		if (_muffled != null)
			AudioTuning.ApplySpatial(_muffled, _radiusTiles);
	}

	void Update()
	{
		if (Time.time < _nextOcclusionCheck)
			return;

		_nextOcclusionCheck = Time.time + AudioTuning.OcclusionRefreshSeconds;

		if (_listener != null)
			_wallCount = MapRaycast.CountWalls(_listener.position, transform.position);
	}

	public void PlayScheduled(double dspTime, float pitch = 1.0f)
	{
		EnsureSources();

		float clearWeight = AudioTuning.ClearWeight(_wallCount);
		float bus = 1.0f;

		Fire(_clear, dspTime, pitch, bus * clearWeight);
		Fire(_muffled, dspTime, pitch, bus * (1.0f - clearWeight));
	}

	void Fire(AudioSource source, double dspTime, float pitch, float volume)
	{
		if (source == null || AudioTuning.IsReady(source.clip) == false)
			return;

		if (volume <= 0.001f)
			return;

		source.Stop();
		source.pitch = Mathf.Max(0.01f, pitch);
		source.volume = volume;
		source.PlayScheduled(dspTime);
	}

	public void SetLoopGain(float gain)
	{
		EnsureSources();

		float clearWeight = AudioTuning.ClearWeight(_wallCount);
		float bus = 1.0f;

		SetLoop(_clear, bus * clearWeight * gain);
		SetLoop(_muffled, bus * (1.0f - clearWeight) * gain);
	}

	void SetLoop(AudioSource source, float volume)
	{
		if (source == null || AudioTuning.IsReady(source.clip) == false)
			return;

		source.loop = true;
		source.volume = volume;

		if (volume > 0.001f && source.isPlaying == false)
			source.Play();
		else if (volume <= 0.001f && source.isPlaying)
			source.Stop();
	}
}
