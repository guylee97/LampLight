using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour, IInteractable
{
	[SerializeField]
	Sprite _lockedSprite;

	[SerializeField]
	Sprite _openSprite;

	SpriteRenderer _renderer;
	StageProgress _progress;
	bool _isOpen;
	bool _escaped;

	public Action OnOpened;
	public Action OnEscaped;

	public bool IsOpen { get { return _isOpen; } }

	public bool CanInteract { get { return _escaped == false; } }

	public string Prompt
	{
		get
		{
			if (_isOpen)
				return "[E] 탈출";

			int remaining = _progress == null ? 0 : _progress.Required - _progress.Collected;
			return $"유물 {remaining}개 더 필요";
		}
	}

	public Vector3 Position { get { return transform.position; } }

	public void Init(StageProgress progress)
	{
		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		if (_progress != null)
			_progress.OnAllArtifactsCollected -= Open;

		_progress = progress;

		if (_progress != null)
			_progress.OnAllArtifactsCollected += Open;

		_escaped = false;
		Close();
	}

	public void Interact(PlayerController player)
	{
		if (_isOpen == false)
			return;

		Escape();
	}

	public void Open()
	{
		if (_isOpen)
			return;

		_isOpen = true;
		ApplySprite();
		Managers.Sound.PlayAtPointOptional(
			"exit_unlock",
			"Open the door (1)",
			transform.position,
			Define.Sound.Guide
		);

		if (OnOpened != null)
			OnOpened.Invoke();
	}

	public void Close()
	{
		_isOpen = false;
		ApplySprite();
	}

	void Escape()
	{
		if (_escaped)
			return;

		_escaped = true;

		if (OnEscaped != null)
			OnEscaped.Invoke();

		Managers.Game.ReportEscaped();
	}

	void ApplySprite()
	{
		if (_renderer == null)
			return;

		Sprite target = _isOpen ? _openSprite : _lockedSprite;
		if (target != null)
			_renderer.sprite = target;
	}

	void OnDestroy()
	{
		if (_progress != null)
			_progress.OnAllArtifactsCollected -= Open;
	}
}
