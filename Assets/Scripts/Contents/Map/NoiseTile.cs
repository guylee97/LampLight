using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NoiseTile : MonoBehaviour
{
	[SerializeField]
	string _clip = "step_glass";

	[SerializeField]
	int _variants = 3;

	[SerializeField]
	float _noiseRadius = 8.0f;

	[SerializeField]
	float _sneakScale = 1.0f;

	[SerializeField]
	float _noiseDuration = 0.6f;

	[SerializeField]
	float _cooldown = 0.8f;

	float _nextTime;

	public string Clip { get { return _clip; } }

	public void Configure(string clip, float radius, float sneakScale)
	{
		_clip = clip;
		_noiseRadius = radius;
		_sneakScale = sneakScale;
	}

	void Reset()
	{
		Collider2D collider = GetComponent<Collider2D>();
		if (collider != null)
			collider.isTrigger = true;
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		Trigger(other);
	}

	void OnTriggerStay2D(Collider2D other)
	{
		Trigger(other);
	}

	string PickClip()
	{
		if (_variants <= 1)
			return _clip;

		int index = Random.Range(1, _variants + 1);
		return index == 1 ? _clip : $"{_clip}_{index}";
	}

	void Trigger(Collider2D other)
	{
		if (Time.time < _nextTime)
			return;

		PlayerController player = other.GetComponentInParent<PlayerController>();
		if (player == null)
			return;

		if (player.State != Define.State.Moving)
			return;

		_nextTime = Time.time + _cooldown;

		float radius = player.IsSneaking ? _noiseRadius * _sneakScale : _noiseRadius;
		if (radius <= 0.0f)
			return;

		Managers.Sound.PlayAtPointOptional(PickClip(), transform.position, Define.Sound.Self);
		player.EmitNoise(radius, _noiseDuration);
	}
}
