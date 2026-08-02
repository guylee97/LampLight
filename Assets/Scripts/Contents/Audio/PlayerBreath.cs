using UnityEngine;

public class PlayerBreath : MonoBehaviour
{
	[SerializeField]
	AudioClip _clip;

	[SerializeField]
	PlayerStatus _status;

	[SerializeField]
	float _threshold = 0.5f;

	[SerializeField]
	float _maxVolume = 0.85f;

	[SerializeField]
	float _fadeSpeed = 2.5f;

	AudioSource _source;
	float _volume;

	public float Volume { get { return _volume; } }

	public static float TargetVolume(float staminaRatio, float threshold)
	{
		if (threshold <= 0.0f || staminaRatio >= threshold)
			return 0.0f;

		return Mathf.Clamp01((threshold - staminaRatio) / threshold);
	}

	void Awake()
	{
		if (_status == null)
			_status = GetComponentInParent<PlayerStatus>();

		if (_clip == null)
			_clip = Managers.Resource.Load<AudioClip>("Sounds/breath_tired");

		_source = Util.GetOrAddComponent<AudioSource>(gameObject);
		_source.clip = _clip;
		_source.loop = true;
		_source.playOnAwake = false;
		_source.spatialBlend = 0.0f;
		_source.volume = 0.0f;
	}

	void Update()
	{
		if (_source == null || _status == null || AudioTuning.IsReady(_source.clip) == false)
			return;

		float target = TargetVolume(_status.StaminaRatio, _threshold);
		_volume = Mathf.MoveTowards(_volume, target, _fadeSpeed * Time.deltaTime);

		_source.volume = _volume * _maxVolume * 1.0f;

		if (_volume > 0.01f && _source.isPlaying == false)
			_source.Play();
		else if (_volume <= 0.01f && _source.isPlaying)
			_source.Stop();
	}
}
