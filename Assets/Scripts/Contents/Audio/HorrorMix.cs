using UnityEngine;

public class HorrorMix : MonoBehaviour
{
	public const string StingerPath = "chase_stinger";
	public const string StingerFallbackPath = "moster growl (4)";

	// 추격 중에는 루프를 깔지 않는다. 정적이 기준선이고, 압박은 심박음이 만든다.
	// 예전 chase_loop 은 웃음소리처럼 들려서 뺐다.
	public const string ChaseLoopPath = "";
	public const string RitualLoopPath = "ritual_loop";

	const float SilenceSeconds = 0.12f;
	const float ReleaseSeconds = 0.45f;

	static HorrorMix s_instance;

	int _chaseCount;
	float _loopStartAt = -1.0f;
	float _releaseAt = -1.0f;
	AudioSource _loop;
	AudioClip _pendingLoopClip;

	public static bool IsChasing { get { return s_instance != null && s_instance._chaseCount > 0; } }

	public static void EnterChase()
	{
		HorrorMix mix = Resolve();
		if (mix == null)
			return;

		mix._chaseCount++;
		mix._releaseAt = -1.0f;

		if (mix._chaseCount != 1)
			return;

		mix.Strike(ChaseLoopPath);
	}

	public static void ExitChase()
	{
		if (s_instance == null)
			return;

		s_instance._chaseCount = Mathf.Max(0, s_instance._chaseCount - 1);

		if (s_instance._chaseCount == 0)
			s_instance._releaseAt = Time.time + ReleaseSeconds;
	}

	public static void ResetState()
	{
		if (s_instance == null)
			return;

		s_instance._chaseCount = 0;
		s_instance._releaseAt = -1.0f;
		s_instance._loopStartAt = -1.0f;
		s_instance.CutLoop();
	}

	public static void PlayRitualLoop()
	{
		HorrorMix mix = Resolve();
		if (mix != null)
			mix.Strike(RitualLoopPath);
	}

	public static void StopRitualLoop()
	{
		if (s_instance != null && s_instance._chaseCount == 0)
			s_instance.CutLoop();
	}

	static HorrorMix Resolve()
	{
		if (s_instance != null)
			return s_instance;

		if (Application.isPlaying == false)
			return null;

		GameObject go = new GameObject("@HorrorMix");
		s_instance = go.AddComponent<HorrorMix>();
		return s_instance;
	}

	void Awake()
	{
		if (s_instance != null && s_instance != this)
		{
			Destroy(gameObject);
			return;
		}

		s_instance = this;
		_chaseCount = 0;

		_loop = gameObject.AddComponent<AudioSource>();
		_loop.playOnAwake = false;
		_loop.loop = true;
		_loop.spatialBlend = 0.0f;
		_loop.volume = 0.0f;
		Managers.Sound.ConfigureSource(_loop, Define.Sound.Bgm, false);
	}

	void OnDestroy()
	{
		if (s_instance == this)
			s_instance = null;
	}

	/// loopPath 가 비면 스팅어 한 방만 치고 다시 정적으로 돌아간다.
	void Strike(string loopPath)
	{
		CutLoop();

		Managers.Sound.PlayOptional(StingerPath, StingerFallbackPath, Define.Sound.Threat);

		if (string.IsNullOrEmpty(loopPath))
			return;

		_pendingLoopClip = Managers.Resource.Load<AudioClip>("Audio/" + loopPath);
		_loopStartAt = _pendingLoopClip == null ? -1.0f : Time.time + SilenceSeconds;
	}

	void CutLoop()
	{
		_loopStartAt = -1.0f;
		_pendingLoopClip = null;

		if (_loop == null)
			return;

		_loop.Stop();
		_loop.clip = null;
	}

	void Update()
	{
		if (_releaseAt > 0.0f && Time.time >= _releaseAt)
		{
			_releaseAt = -1.0f;

			if (_chaseCount == 0)
				CutLoop();
		}

		if (_loopStartAt < 0.0f || Time.time < _loopStartAt || _loop == null)
			return;

		_loopStartAt = -1.0f;

		if (_pendingLoopClip == null)
			return;

		_loop.clip = _pendingLoopClip;
		_loop.volume = 1.0f;
		_loop.Play();
		_pendingLoopClip = null;
	}
}
