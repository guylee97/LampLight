using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Container : MonoBehaviour, IInteractable
{
	public const string OpenClip = "container_open";
	public const string HoldClip = "container_hold";
	public const string OpenSuffix = "_open";
	public const string ClosedSuffix = "_closed";

	[SerializeField]
	string _closedKey;

	[SerializeField]
	float _holdSeconds = 2.0f;

	[SerializeField]
	float _noiseRadius = 5.0f;

	[SerializeField]
	float _noiseDuration = 0.5f;

	[SerializeField]
	SpriteRenderer _renderer;

	bool _opened;

	public Action<Container> OnOpened;

	public bool IsOpened { get { return _opened; } }

	public bool CanInteract { get { return _opened == false && OpenKey != null; } }
	public string Prompt { get { return "[E] 열기"; } }
	public Vector3 Position { get { return transform.position; } }
	public float HoldSeconds { get { return _holdSeconds; } }

	string OpenKey
	{
		get
		{
			if (string.IsNullOrEmpty(_closedKey) || _closedKey.EndsWith(ClosedSuffix) == false)
				return null;

			return _closedKey.Substring(0, _closedKey.Length - ClosedSuffix.Length) + OpenSuffix;
		}
	}

	public void Configure(string closedKey, SpriteRenderer renderer)
	{
		_closedKey = closedKey;
		_renderer = renderer;
	}

	public void Interact(PlayerController player)
	{
		if (_opened || player == null)
			return;

		string openKey = OpenKey;
		if (openKey == null)
			return;

		TempleObject entry = TempleManifest.Catalog == null
			? null
			: TempleManifest.Catalog.Object(openKey);

		if (entry == null)
			return;

		Sprite sprite = Resources.Load<Sprite>(entry.resource);
		if (sprite == null)
			return;

		_opened = true;

		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		if (_renderer != null)
			_renderer.sprite = sprite;

		Managers.Sound.PlayAtPointOptional(
			"Wood_Drawer/floraphonic-wooden-trunk-latch-1-183944",
			OpenClip,
			transform.position,
			Define.Sound.Self);
		player.EmitNoise(_noiseRadius, _noiseDuration);

		if (OnOpened != null)
			OnOpened.Invoke(this);
	}
}
