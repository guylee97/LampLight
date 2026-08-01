using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Artifact : MonoBehaviour, IInteractable
{
	[SerializeField]
	string _pointName;

	[SerializeField]
	SpriteRenderer _renderer;

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

	public bool CanInteract { get { return _collected == false; } }
	public string Prompt { get { return "[E] 유물 수집"; } }
	public Vector3 Position { get { return transform.position; } }

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

	public void Interact(PlayerController player)
	{
		if (TryCollect() == false)
			return;

		if (player != null)
			player.EmitNoise(_collectNoiseRadius, _collectNoiseDuration);
	}

	public bool TryCollect()
	{
		if (_collected)
			return false;

		_collected = true;

		if (_progress != null)
			_progress.ReportCollected();

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
