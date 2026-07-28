using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Artifact : MonoBehaviour
{
	[SerializeField]
	string _pointName;

	[SerializeField]
	SpriteRenderer _renderer;

	[SerializeField]
	float _collectNoiseRadius = 12.0f;

	StageProgress _progress;
	bool _collected;
	bool _playerInRange;

	public Action<Artifact> OnCollected;

	public string PointName { get { return _pointName; } }
	public bool IsCollected { get { return _collected; } }
	public bool PlayerInRange { get { return _playerInRange; } }
	public bool CanCollect { get { return _collected == false && _playerInRange; } }
	public float CollectNoiseRadius { get { return _collectNoiseRadius; } }

	public void Init(StageProgress progress, string pointName)
	{
		_progress = progress;
		_pointName = pointName;
		_collected = false;
		_playerInRange = false;
	}

	public bool TryCollect()
	{
		if (CanCollect == false)
			return false;

		_collected = true;
		_playerInRange = false;

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

	bool IsPlayer(Collider2D other)
	{
		BaseController controller = other.GetComponentInParent<BaseController>();
		return controller != null && controller.WorldObjectType == Define.WorldObject.Player;
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (_collected == false && IsPlayer(other))
			_playerInRange = true;
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (IsPlayer(other))
			_playerInRange = false;
	}
}
