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
	float _hold;
	AudioSource _holdSource;

	public Action<IInteractable> OnTargetChanged;

	public IInteractable Current { get { return _current; } }

	public float HoldProgress
	{
		get
		{
			if (_current == null || _current.HoldSeconds <= 0.0f)
				return 0.0f;

			return Mathf.Clamp01(_hold / _current.HoldSeconds);
		}
	}

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
		UpdateHold(pressed);
		_wasPressed = pressed;
	}

	void UpdateHold(bool pressed)
	{
		if (_current == null || _current.CanInteract == false || pressed == false)
		{
			StopHold();
			return;
		}

		if (_current.HoldSeconds <= 0.0f)
		{
			if (_wasPressed == false)
				_current.Interact(_player);

			return;
		}

		if (_hold <= 0.0f)
			StartHold();

		_hold += Time.deltaTime;

		if (_hold < _current.HoldSeconds)
			return;

		IInteractable target = _current;
		StopHold();
		target.Interact(_player);
	}

	void StartHold()
	{
		if (_holdSource == null)
		{
			_holdSource = Util.GetOrAddComponent<AudioSource>(gameObject);
			_holdSource.clip = Managers.Resource.Load<AudioClip>("Sounds/container_hold");
			_holdSource.loop = true;
			_holdSource.playOnAwake = false;
			_holdSource.spatialBlend = 0.0f;
		}

		if (_holdSource.clip != null && _holdSource.isPlaying == false)
		{
			_holdSource.volume = 1.0f;
			_holdSource.Play();
		}
	}

	void StopHold()
	{
		_hold = 0.0f;

		if (_holdSource != null && _holdSource.isPlaying)
			_holdSource.Stop();
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
		StopHold();

		if (OnTargetChanged != null)
			OnTargetChanged.Invoke(target);
	}
}
