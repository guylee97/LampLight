using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour
{
	[SerializeField]
	Sprite _lockedSprite;

	[SerializeField]
	Sprite _openSprite;

	[SerializeField]
	LayerMask _playerMask;

	SpriteRenderer _renderer;
	StageProgress _progress;
	bool _isOpen;

	public Action OnOpened;
	public Action OnEscaped;

	public bool IsOpen { get { return _isOpen; } }

	public void Init(StageProgress progress)
	{
		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		if (_progress != null)
			_progress.OnAllArtifactsCollected -= Open;

		_progress = progress;

		if (_progress != null)
			_progress.OnAllArtifactsCollected += Open;

		Close();
	}

	public void Open()
	{
		if (_isOpen)
			return;

		_isOpen = true;
		ApplySprite();

		if (OnOpened != null)
			OnOpened.Invoke();
	}

	public void Close()
	{
		_isOpen = false;
		ApplySprite();
	}

	void ApplySprite()
	{
		if (_renderer == null)
			return;

		Sprite target = _isOpen ? _openSprite : _lockedSprite;
		if (target != null)
			_renderer.sprite = target;
	}

	bool IsPlayer(Collider2D other)
	{
		return (_playerMask.value & (1 << other.gameObject.layer)) != 0;
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (_isOpen == false || IsPlayer(other) == false)
			return;

		if (OnEscaped != null)
			OnEscaped.Invoke();
	}

	void OnDestroy()
	{
		if (_progress != null)
			_progress.OnAllArtifactsCollected -= Open;
	}
}
