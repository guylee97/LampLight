using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour, IInteractable
{

	public const string UnlockClip = "exit_unlock";
	public const string CreakClip = "door_creak";

	[SerializeField]
	Sprite _lockedSprite;

	[SerializeField]
	Sprite _openSprite;

	[SerializeField]
	PingScheduler _ping;

	SpriteRenderer _renderer;
	SpriteRenderer _art;
	StageProgress _progress;
	PlayerController _player;
	Vector2Int _doorway = new Vector2Int(-1, -1);
	bool _isOpen;
	bool _escaped;

	public Action OnOpened;
	public Action OnEscaped;

	public bool IsOpen { get { return _isOpen; } }

	public bool CanInteract { get { return _escaped == false && _isOpen; } }

	public bool PlayerIsInDoorway
	{
		get
		{
			if (_doorway.x < 0)
				return true;

			if (_player == null)
				_player = FindFirstObjectByType<PlayerController>();

			if (_player == null)
				return false;

			Vector2Int tile = MapCoord.WorldToTile(_player.transform.position);
			return tile.y == _doorway.y
				&& Mathf.Abs(tile.x - _doorway.x) <= WallFaceRules.DoorCols / 2;
		}
	}

	public string Prompt
	{
		get
		{
			if (_isOpen)
				return "[E] \uB2E4\uC74C \uAD6C\uC5ED\uC73C\uB85C \uC774\uB3D9";

			int remaining = _progress == null ? 0 : _progress.Required - _progress.Collected;
			return $"\uC720\uBB3C {remaining}\uAC1C\uAC00 \uB354 \uD544\uC694\uD558\uB2E4";
		}
	}

	public Vector3 Position { get { return transform.position; } }
	public float HoldSeconds { get { return 0.0f; } }

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

		if (_progress != null && _progress.IsComplete)
			Open();
	}

	public void Interact(PlayerController player)
	{
		if (_isOpen == false)
			return;

		Escape(player);
	}

	public void Open()
	{
		if (_isOpen)
			return;

		_isOpen = true;
		ApplySprite();
		ApplyPing();
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
		ApplyPing();
	}

	void ApplyPing()
	{
		if (_ping != null)
			_ping.Active = _isOpen;
	}

	void Escape(PlayerController player)
	{
		if (_escaped)
			return;

		_escaped = true;
		Managers.Sound.PlayAtPointOptional(
			"exit_stairs",
			CreakClip,
			transform.position,
			Define.Sound.Guide
		);

		if (OnEscaped != null)
			OnEscaped.Invoke();

		int collected = _progress != null ? _progress.Collected : 0;
		float weighted = _progress != null ? _progress.WeightedValue : 0.0f;
		float lampRemaining = 0.0f;

		if (player != null && player.Lamp != null)
			lampRemaining = player.Lamp.RemainingDuration;

		Managers.Game.ReportEscaped(collected, weighted, lampRemaining);
	}

	public void UseCatalogSprites(MapPoint point)
	{
		Sprite locked = MapObjectPlacer.LoadObjectSprite(DecoSpec.ExitLocked);
		Sprite open = MapObjectPlacer.LoadObjectSprite(DecoSpec.ExitOpen);

		if (locked != null)
			_lockedSprite = locked;

		if (open != null)
			_openSprite = open;

		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		_art = _renderer;

		ApplySprite();
	}

	public void UseStairSprite()
	{
		_renderer = GetComponent<SpriteRenderer>();
		_art = _renderer;
		Sprite stair = Resources.Load<Sprite>(
			"Art/Objects/large/obj_large_stairwell_down");
		if (stair != null)
		{
			_lockedSprite = stair;
			_openSprite = stair;
		}
		_doorway = new Vector2Int(-1, -1);
		ApplySprite();
		ConfigureExitPing();
	}

	void ConfigureExitPing()
	{
		OcclusionSource source = GetComponent<OcclusionSource>();
		if (source == null)
			return;

		AudioClip clear = Resources.Load<AudioClip>("Audio/artifact_ping_3");
		if (clear == null)
			clear = Resources.Load<AudioClip>("Audio/artifact_ping");

		AudioClip muffled = Resources.Load<AudioClip>("Audio/artifact_ping_2");
		if (muffled == null)
			muffled = clear;

		source.Configure(clear, muffled, Define.Sound.Guide, 9.0f);

		if (_ping != null)
			_ping.RadiusTiles = 9.0f;
	}




	void ApplySprite()
	{
		if (_art == null)
			_art = _renderer;

		if (_art == null)
			return;

		Sprite target = _isOpen ? _openSprite : _lockedSprite;
		if (target != null)
			_art.sprite = target;
	}

	void OnDestroy()
	{
		if (_progress != null)
			_progress.OnAllArtifactsCollected -= Open;
	}
}
