using System.Collections.Generic;
using UnityEngine;

public class MapDecoPlacer : MonoBehaviour
{
	public const int DecoSortingOrder = 10;

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

			float halfHeight = sprite.bounds.size.y * 0.5f;
			float tileBottom = map.height - placement.TileY - 0.5f;
			go.transform.position = new Vector3(
				placement.TileX, tileBottom + halfHeight, 0.0f);

			SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
			renderer.sprite = sprite;
			renderer.sortingLayerName = _sortingLayer;
			renderer.sortingOrder = DecoSortingOrder;
			renderer.spriteSortPoint = SpriteSortPoint.Pivot;

			AttachNoiseTrigger(go, placement.Key);
			AttachContainer(go, placement.Key, renderer);

			_spawned.Add(go);
		}
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
