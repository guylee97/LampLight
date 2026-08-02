using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour, IInteractable
{
	public const float WallDrawOffset = 1.0f;
	public const int WallSortingOrder = 10;

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

	public bool CanInteract { get { return _escaped == false && PlayerIsInDoorway; } }

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
				return "[E] 탈출";

			int remaining = _progress == null ? 0 : _progress.Required - _progress.Collected;
			return $"유물 {remaining}개 더 필요";
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

		if (_progress != null && _progress.Required <= 0)
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
		Managers.Sound.PlayAtPointOptional(CreakClip, transform.position, Define.Sound.Guide);

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

		if (_renderer != null && point != null)
			PlaceOnWallFace(point);
		else
			_art = _renderer;

		ApplySprite();
	}

	void PlaceOnWallFace(MapPoint point)
	{
		MapData map = Managers.Data.Map;
		TempleObject entry = TempleManifest.IsReady
			? TempleManifest.Catalog.Object(DecoSpec.ExitLocked)
			: null;

		_art = _renderer;

		if (map == null || entry == null)
			return;

		int cols = Mathf.Max(1, Mathf.RoundToInt(entry.w / 32.0f));
		int rows = Mathf.Max(1, Mathf.RoundToInt(entry.h / 32.0f));

		if (WallFaceRules.BlockedBand(map, point.col, point.row, cols, rows) == false)
		{
			Debug.LogError($"ExitDoor: ({point.col},{point.row}) 위쪽에 {cols}x{rows} 벽면이 없다");
			return;
		}

		_doorway = new Vector2Int(point.col, point.row);
		ShrinkTrigger(cols);

		_art = BuildArt();
		_renderer.enabled = false;
		_renderer.sprite = null;

		Vector2 basePoint = WallFaceRules.BaseOf(map, point.col, point.row);
		_art.transform.position = new Vector3(basePoint.x, basePoint.y, 0.0f);
		_art.sortingLayerID = _renderer.sortingLayerID;
		_art.sortingOrder = WallSortingOrder;
	}

	void ShrinkTrigger(int cols)
	{
		BoxCollider2D box = GetComponent<BoxCollider2D>();
		if (box == null)
			return;

		box.size = new Vector2(cols, 1.0f);
		box.offset = Vector2.zero;
	}

	SpriteRenderer BuildArt()
	{
		Transform existing = transform.Find("Art");
		if (existing != null)
			return existing.GetComponent<SpriteRenderer>();

		GameObject art = new GameObject("Art");
		art.transform.SetParent(transform, false);

		SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
		renderer.spriteSortPoint = SpriteSortPoint.Pivot;
		renderer.sharedMaterial = _renderer.sharedMaterial;
		return renderer;
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
