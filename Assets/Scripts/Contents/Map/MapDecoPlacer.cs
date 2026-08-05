using System.Collections.Generic;
using UnityEngine;

public class MapDecoPlacer : MonoBehaviour
{
	public const int DecoSortingOrder = 20;

	[SerializeField]
	Transform _root;

	[SerializeField]
	string _sortingLayer = "Default";

	[SerializeField]
	bool _placeOnStart;


	readonly List<GameObject> _spawned = new List<GameObject>();
	readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

	public int Count { get { return _spawned.Count; } }

	void Start()
	{
		if (_placeOnStart)
			Place(0);
	}

	public void Place(int seed)
	{
		Place(Managers.Data.Map, seed);
	}

	public void Place(MapData map, int seed)
	{
		Clear();

		if (map == null)
			return;

		if (map.decorations != null && map.decorations.Length > 0)
		{
			PlaceFixed(map);
			return;
		}

		List<DecoPlacement> plan = MapDecoPlan.Build(map, seed);
		if (plan.Count == 0)
			return;

		Transform parent = _root != null ? _root : transform;

		foreach (DecoPlacement placement in plan)
		{
			Sprite sprite = Load(placement.Key);
			if (sprite == null)
				continue;

			GameObject go = new GameObject(placement.Key);
			go.transform.SetParent(parent, false);

			Bounds local = sprite.bounds;
			float tileBottom = map.height - placement.TileY - 0.5f;
			go.transform.position = new Vector3(
				placement.TileX - (local.min.x + local.max.x) * 0.5f,
				tileBottom - local.min.y, 0.0f);

			SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();

			if (placement.Key.StartsWith(MapDecoPlan.CategoryWallDeco))
			{
				renderer.color = WallTint;
			}
			else
			{
				Material lit = LitMaterial();
				if (lit != null)
					renderer.sharedMaterial = lit;
			}

			renderer.sprite = sprite;
			renderer.sortingLayerName = _sortingLayer;
			renderer.sortingOrder = IsGroundDecoration(placement.Key)
				? -900
				: WorldYSort.OrderFor(tileBottom);
			renderer.spriteSortPoint = SpriteSortPoint.Pivot;

			AttachNoiseTrigger(go, placement.Key);
			AttachContainer(go, placement.Key, renderer);

			_spawned.Add(go);
		}
	}

	void PlaceFixed(MapData map)
	{
		Transform parent = _root != null ? _root : transform;

		foreach (MapDecoration placement in map.decorations)
		{
			Sprite sprite = Resources.Load<Sprite>(placement.resource);
			if (sprite == null)
			{
				Debug.LogWarning($"MapDecoPlacer: Resources/{placement.resource} 로드 실패");
				continue;
			}

			GameObject go = new GameObject(placement.key);
			go.transform.SetParent(parent, false);

			Bounds bounds = sprite.bounds;
			float trim = DecoSpec.DisplayScale(placement.key);
			float scaleX = bounds.size.x > 0.0f ? placement.width * trim / bounds.size.x : 1.0f;
			float scaleY = bounds.size.y > 0.0f ? placement.height * trim / bounds.size.y : 1.0f;

			go.transform.localScale = new Vector3(scaleX, scaleY, 1.0f);
			go.transform.position = new Vector3(
				placement.x + placement.width * 0.5f - bounds.center.x * scaleX,
				map.height - placement.y + placement.height * 0.5f - bounds.center.y * scaleY,
				0.0f);

			SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
			renderer.sprite = sprite;
			renderer.sortingLayerName = _sortingLayer;
			float visualBottomY = map.height - placement.y;
			renderer.sortingOrder = IsGroundDecoration(placement.key)
				? -900 + placement.sortingOffset
				: WorldYSort.OrderFor(visualBottomY) + placement.sortingOffset;
			renderer.spriteSortPoint = SpriteSortPoint.Pivot;

			if (placement.flipDiagonal)
			{
				go.transform.Rotate(0.0f, 0.0f, 90.0f);
				renderer.flipX = placement.flipVertical;
				renderer.flipY = placement.flipHorizontal;
			}
			else
			{
				renderer.flipX = placement.flipHorizontal;
				renderer.flipY = placement.flipVertical;
			}

			Material lit = LitMaterial();
			if (lit != null)
				renderer.sharedMaterial = lit;

			AttachFixedCollision(go, placement, scaleX, scaleY);
			AttachNoiseTrigger(go, placement.key);
			AttachContainer(go, placement.key, renderer);
			_spawned.Add(go);
		}
	}

	public const float MapObjectClearance = 0.35f;

	void AttachFixedCollision(GameObject go, MapDecoration placement, float scaleX, float scaleY)
	{
		if (placement.collisionEnabled == false
			|| placement.colliderWidth <= 0.0f || placement.colliderHeight <= 0.0f)
			return;

		if (SealsMapObject(placement))
		{
			Debug.LogWarning($"MapDecoPlacer: '{placement.key}' 가 필수 지점을 막아 충돌을 껐다");
			return;
		}

		float absScaleX = Mathf.Max(0.0001f, Mathf.Abs(scaleX));
		float absScaleY = Mathf.Max(0.0001f, Mathf.Abs(scaleY));
		float visualBottomX = placement.x + placement.width * 0.5f;
		float visualBottomY = Managers.Data.Map.height - placement.y;
		Vector2 worldCenter = new Vector2(
			visualBottomX + placement.colliderOffsetX,
			visualBottomY + placement.colliderOffsetY);

		BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
		collider.size = new Vector2(
			placement.colliderWidth / absScaleX,
			placement.colliderHeight / absScaleY);
		collider.offset = new Vector2(
			(worldCenter.x - go.transform.position.x) / scaleX,
			(worldCenter.y - go.transform.position.y) / scaleY);
	}

	public static bool SealsMapObject(MapDecoration placement)
	{
		MapData map = Managers.Data.Map;
		if (map == null || map.objects == null)
			return false;

		float centerX = placement.x + placement.width * 0.5f + placement.colliderOffsetX;
		float centerY = map.height - placement.y + placement.colliderOffsetY;
		float halfWidth = placement.colliderWidth * 0.5f;
		float halfHeight = placement.colliderHeight * 0.5f;

		foreach (MapPoint point in map.objects)
		{
			Vector3 world = MapCoord.ToWorld(point);

			if (Mathf.Abs(world.x - centerX) < halfWidth + MapObjectClearance
				&& Mathf.Abs(world.y - centerY) < halfHeight + MapObjectClearance)
				return true;
		}

		return false;
	}

	static bool IsGroundDecoration(string key)
	{
		return key.StartsWith("prop_floor_")
			|| key.StartsWith("large_carpet_")
			|| key.StartsWith("cobweb_")
			|| key.StartsWith("extra_cobweb_")
			|| key.StartsWith("debris_")
			|| key.StartsWith("noise_")
			|| key == "prop_grate"
			|| key == "prop_pebbles"
			|| key == "prop_roots"
			|| key == "prop_roots_stone"
			|| key == "prop_bones_long"
			|| key == "prop_bones_pile"
			|| key == "prop_skull"
			|| key == "extra_prop_skull_b"
			|| key.StartsWith("prop_candle_");
	}

	public static readonly Color WallTint = new Color(0.25f, 0.26f, 0.28f, 1.0f);

	static Material _lit;

	static Material LitMaterial()
	{
		if (_lit == null)
			_lit = Resources.Load<Material>("Image/M_SpriteLit");

		return _lit;
	}

	public void Clear()
	{
		foreach (GameObject go in _spawned)
		{
			if (go == null)
				continue;

			if (Application.isPlaying)
				Destroy(go);
			else
				DestroyImmediate(go);
		}

		_spawned.Clear();
	}

	void AttachNoiseTrigger(GameObject go, string key)
	{
		TempleObject entry = TempleManifest.Catalog.Object(key);
		if (entry == null || entry.category != MapDecoPlan.CategoryNoise)
			return;

		BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
		collider.isTrigger = true;
		collider.size = new Vector2(entry.Cols, entry.Rows);
		collider.offset = new Vector2(0.0f, entry.Rows * 0.5f);

		NoiseTile tile = go.AddComponent<NoiseTile>();

		if (key == "noise_glass")
			tile.Configure("step_glass", DecoSpec.GlassNoiseTiles, DecoSpec.GlassSneakScale);
		else
			tile.Configure("step_noisy_floor", DecoSpec.PlankNoiseTiles, 1.0f);
	}

	void AttachContainer(GameObject go, string key, SpriteRenderer renderer)
	{
		if (key.EndsWith(Container.ClosedSuffix) == false)
			return;

		TempleObject entry = TempleManifest.Catalog.Object(key);
		if (entry == null || entry.category != MapDecoPlan.CategoryContainer)
			return;

		BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
		collider.isTrigger = true;
		collider.size = new Vector2(entry.Cols, entry.Rows);
		collider.offset = new Vector2(0.0f, entry.Rows * 0.5f);

		Container container = go.AddComponent<Container>();
		container.Configure(key, renderer);
	}

	Sprite Load(string key)
	{
		Sprite cached;
		if (_sprites.TryGetValue(key, out cached))
			return cached;

		TempleObject entry = TempleManifest.Catalog == null ? null : TempleManifest.Catalog.Object(key);
		if (entry == null)
		{
			Debug.LogWarning($"MapDecoPlacer: 카탈로그에 '{key}' 없음");
			_sprites[key] = null;
			return null;
		}

		Sprite sprite = Resources.Load<Sprite>(entry.resource);
		if (sprite == null)
			Debug.LogWarning($"MapDecoPlacer: Resources/{entry.resource} 로드 실패");

		_sprites[key] = sprite;
		return sprite;
	}
}
