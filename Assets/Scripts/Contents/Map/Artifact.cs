using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Artifact : MonoBehaviour, IInteractable
{
	[SerializeField]
	string _pointName;

	[SerializeField]
	SpriteRenderer _renderer;

	[SerializeField, Range(0, 2)]
	int _concealment;

	[SerializeField]
	float _collectNoiseRadius = 12.0f;

	[SerializeField]
	float _collectNoiseDuration = 1.5f;

	[SerializeField]
	float _guideRadius = 12.0f;

	[SerializeField]
	float _guideSecondsPerUnit = 0.3f;

	StageProgress _progress;
	bool _collected;
	float _nextGuideTime;

	public Action<Artifact> OnCollected;

	public string PointName { get { return _pointName; } }
	public bool IsCollected { get { return _collected; } }
	public float CollectNoiseRadius { get { return _collectNoiseRadius; } }

	public int Concealment { get { return _concealment; } }
	public float HoldSeconds { get { return ConcealmentRules.HoldSeconds(_concealment); } }
	public float RadiusScale { get { return ConcealmentRules.RadiusScale(_concealment); } }
	public float ScoreWeight { get { return ConcealmentRules.ScoreWeight(_concealment); } }
	public float NoiseRadius { get { return ConcealmentRules.NoiseRadius(_concealment); } }

	public bool CanInteract { get { return _collected == false; } }

	public string Prompt
	{
		get
		{
			if (_concealment == 0)
				return "[E] 유물 수집";

			return _concealment == 1 ? "[E] 잔해를 헤친다" : "[E] 석관을 연다";
		}
	}

	public Vector3 Position { get { return transform.position; } }

	public void SetConcealment(int level)
	{
		_concealment = Mathf.Clamp(level, 0, 2);
	}

	public void Init(StageProgress progress, string pointName)
	{
		_progress = progress;
		_pointName = pointName;
		_collected = false;
		_nextGuideTime = Time.time;
	}

	void Update()
	{
		if (_collected || Managers.TryGetGame(out GameManagerEx game) == false)
			return;

		GameObject player = game.GetPlayer();
		if (player == null)
			return;

		float distance = Vector2.Distance(transform.position, player.transform.position);
		if (distance > _guideRadius || Time.time < _nextGuideTime)
			return;

		float interval = Mathf.Max(0.25f, distance * _guideSecondsPerUnit);
		float intensity = Mathf.Lerp(1.0f, 0.35f, distance / _guideRadius);
		Managers.Sound.PlayAtPointOptional(
			"artifact_ping",
			transform.position,
			Define.Sound.Guide,
			intensity
		);
		_nextGuideTime = Time.time + interval;
	}

	public void Init(StageProgress progress, string pointName, int concealment)
	{
		Init(progress, pointName);
		SetConcealment(concealment);
	}

	public void Interact(PlayerController player)
	{
		if (TryCollect() == false)
			return;

		if (player != null)
			player.EmitNoise(NoiseRadius, _collectNoiseDuration);
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (_concealment != 0 || _collected)
			return;

		PlayerController player = other.GetComponentInParent<PlayerController>();
		if (player == null)
			return;

		if (TryCollect())
			player.EmitNoise(NoiseRadius, _collectNoiseDuration);
	}

	public bool TryCollect()
	{
		if (_collected)
			return false;

		_collected = true;

		if (_progress != null)
			_progress.ReportCollected(ScoreWeight);

		Managers.Sound.PlayAtPointOptional("artifact_pickup", transform.position, Define.Sound.Self,
			NoiseRadius);

		if (OnCollected != null)
			OnCollected.Invoke(this);

		Managers.Sound.PlayAtPointOptional(
			"artifact_pickup",
			transform.position,
			Define.Sound.Guide
		);

		gameObject.SetActive(false);
		return true;
	}

	public void SetSprite(Sprite sprite)
	{
		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		if (_renderer != null && sprite != null)
			_renderer.sprite = sprite;
	}
}
