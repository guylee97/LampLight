using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class OilCanister : MonoBehaviour, IInteractable
{
	[SerializeField]
	float _refillSeconds = 35.0f;

	[SerializeField]
	SpriteRenderer _renderer;

	[SerializeField]
	Color _emptyTint = new Color(0.35f, 0.32f, 0.28f, 1.0f);

	bool _used;

	public Action<OilCanister> OnUsed;

	public bool IsUsed { get { return _used; } }
	public float RefillSeconds { get { return _refillSeconds; } }

	public bool CanInteract { get { return _used == false; } }
	public string Prompt { get { return "[E] 기름 보충"; } }
	public Vector3 Position { get { return transform.position; } }

	public void Interact(PlayerController player)
	{
		if (_used || player == null)
			return;

		Lamp lamp = player.Lamp;
		if (lamp == null || lamp.Refill(_refillSeconds) == false)
			return;

		_used = true;
		Managers.Sound.PlayAtPointOptional(
			"container_open",
			"Open the door (2)",
			transform.position,
			Define.Sound.Self
		);

		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		if (_renderer != null)
			_renderer.color = _emptyTint;

		if (OnUsed != null)
			OnUsed.Invoke(this);
	}
}
