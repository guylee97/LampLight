using UnityEngine;

[RequireComponent(typeof(OcclusionSource))]
public class ProximityDrone : MonoBehaviour
{
	[SerializeField]
	float _radiusTiles = 6.0f;

	[SerializeField]
	float _maxGain = 0.55f;

	[SerializeField]
	float _falloffPower = 2.0f;

	OcclusionSource _source;
	Transform _listener;
	float _gain = -1.0f;

	public float RadiusTiles { get { return _radiusTiles; } }

	void Awake()
	{
		_source = GetComponent<OcclusionSource>();
		_source.RadiusTiles = _radiusTiles;
	}

	void OnDisable()
	{
		if (_source != null)
			_source.SetLoopGain(0.0f);

		_gain = -1.0f;
	}

	void Update()
	{
		if (_source == null)
			return;

		if (ResolveListener() == false)
		{
			Apply(0.0f);
			return;
		}

		float distance = Vector2.Distance(transform.position, _listener.position);
		if (distance >= _radiusTiles)
		{
			Apply(0.0f);
			return;
		}

		float near = 1.0f - distance / _radiusTiles;
		Apply(Mathf.Pow(near, _falloffPower) * _maxGain);
	}

	bool ResolveListener()
	{
		if (_listener != null)
			return true;

		GameObject player = Managers.Game.GetPlayer();
		if (player == null)
			return false;

		_listener = player.transform;
		_source.SetListener(_listener);
		return true;
	}

	void Apply(float gain)
	{
		if (Mathf.Abs(gain - _gain) < 0.01f)
			return;

		_gain = gain;
		_source.SetLoopGain(gain);
	}
}
