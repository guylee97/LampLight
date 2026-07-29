using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
	[SerializeField]
	float _radius = 1.15f;

	[SerializeField]
	LayerMask _mask = ~0;

	[SerializeField]
	PlayerController _player;

	readonly Collider2D[] _hits = new Collider2D[16];

	ContactFilter2D _filter;
	IInteractable _current;
	bool _wasPressed;

	public Action<IInteractable> OnTargetChanged;

	public IInteractable Current { get { return _current; } }

	void Awake()
	{
		if (_player == null)
			_player = GetComponent<PlayerController>();

		_filter = new ContactFilter2D();
		_filter.useLayerMask = true;
		_filter.layerMask = _mask;
		_filter.useTriggers = true;
	}

	void Update()
	{
		if (Managers.Game.IsPlaying == false)
		{
			SetCurrent(null);
			return;
		}

		SetCurrent(FindNearest());

		Keyboard keyboard = Keyboard.current;
		if (keyboard == null)
			return;

		bool pressed = keyboard.eKey.isPressed;
		if (pressed && _wasPressed == false && _current != null && _current.CanInteract)
			_current.Interact(_player);

		_wasPressed = pressed;
	}

	IInteractable FindNearest()
	{
		int count = Physics2D.OverlapCircle(transform.position, _radius, _filter, _hits);

		IInteractable best = null;
		float bestSqr = float.MaxValue;

		for (int i = 0; i < count; i++)
		{
			Collider2D hit = _hits[i];
			if (hit == null)
				continue;

			IInteractable candidate = hit.GetComponentInParent<IInteractable>();
			if (candidate == null || candidate.CanInteract == false)
				continue;

			float sqr = (candidate.Position - transform.position).sqrMagnitude;
			if (sqr >= bestSqr)
				continue;

			best = candidate;
			bestSqr = sqr;
		}

		return best;
	}

	void SetCurrent(IInteractable target)
	{
		if (ReferenceEquals(_current, target))
			return;

		_current = target;

		if (OnTargetChanged != null)
			OnTargetChanged.Invoke(target);
	}
}
