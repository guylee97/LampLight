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

	StageProgress _progress;
	bool _collected;

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
