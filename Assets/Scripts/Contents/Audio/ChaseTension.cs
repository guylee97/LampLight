using UnityEngine;

public class ChaseTension : MonoBehaviour
{
	public const string HeartbeatClip = "chase_heartbeat";
	public const string HeartbeatFallbackClip = "ritual_step";

	const float NearTiles = 3.0f;
	const float FarTiles = 11.0f;

	static ChaseTension s_instance;

	[SerializeField]
	float _zoomAtNear = 0.82f;

	[SerializeField]
	float _slowestBeat = 0.95f;

	[SerializeField]
	float _fastestBeat = 0.30f;

	CameraController _camera;
	Transform _player;
	AudioSource _heart;
	AudioClip _heartClip;
	float _pressure;
	float _nextBeat;

	public static float Pressure { get { return s_instance == null ? 0.0f : s_instance._pressure; } }

	public static void Ensure()
	{
		if (s_instance != null || Application.isPlaying == false)
			return;

		GameObject go = new GameObject("@ChaseTension");
		s_instance = go.AddComponent<ChaseTension>();
	}

	void Awake()
	{
		if (s_instance != null && s_instance != this)
		{
			Destroy(gameObject);
			return;
		}

		s_instance = this;

		_heart = gameObject.AddComponent<AudioSource>();
		_heart.playOnAwake = false;
		_heart.loop = false;
		_heart.spatialBlend = 0.0f;
		Managers.Sound.ConfigureSource(_heart, Define.Sound.Self, false);

		_heartClip = Managers.Resource.Load<AudioClip>("Audio/" + HeartbeatClip);
		if (_heartClip == null)
			_heartClip = Managers.Resource.Load<AudioClip>("Audio/" + HeartbeatFallbackClip);
	}

	void OnDestroy()
	{
		if (s_instance == this)
			s_instance = null;
	}

	void Update()
	{
		float target = Managers.Game.IsPlaying ? MeasurePressure() : 0.0f;
		float speed = target > _pressure ? 3.5f : 1.2f;
		_pressure = Mathf.MoveTowards(_pressure, target, speed * Time.deltaTime);

		ApplyCamera();
		ApplyHeartbeat();
	}

	float MeasurePressure()
	{
		if (HorrorMix.IsChasing == false)
			return 0.0f;

		if (ResolvePlayer() == false)
			return 0.0f;

		float nearest = float.MaxValue;

		foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
		{
			if (enemy.Awareness != Define.Awareness.Alerted)
				continue;

			float distance = Vector2.Distance(enemy.transform.position, _player.position);
			nearest = Mathf.Min(nearest, distance);
		}

		if (nearest >= float.MaxValue)
			return 0.0f;

		float t = Mathf.InverseLerp(FarTiles, NearTiles, nearest);
		return Ease.SmoothStep(t);
	}

	void ApplyCamera()
	{
		if (_camera == null)
			_camera = FindFirstObjectByType<CameraController>();

		if (_camera == null)
			return;

		_camera.ZoomTo(Mathf.Lerp(1.0f, _zoomAtNear, _pressure), 1.2f);
	}

	void ApplyHeartbeat()
	{
		if (_heart == null || _heartClip == null)
			return;

		if (_pressure <= 0.05f)
		{
			_nextBeat = 0.0f;
			return;
		}

		if (Time.time < _nextBeat)
			return;

		_heart.clip = _heartClip;
		_heart.volume = Mathf.Lerp(0.25f, 1.0f, _pressure);
		_heart.pitch = Mathf.Lerp(0.72f, 0.95f, _pressure);
		_heart.Play();

		_nextBeat = Time.time + Mathf.Lerp(_slowestBeat, _fastestBeat, _pressure);
	}

	bool ResolvePlayer()
	{
		if (_player != null)
			return true;

		GameObject go = Managers.Game.GetPlayer();
		_player = go != null ? go.transform : null;
		return _player != null;
	}
}
