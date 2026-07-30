using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class MapTilemapPainter
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string MapDataPath = "Assets/Resources/Data/MapData.json";
	const string TileAssetDir = "Assets/Resources/Palette/Tiles";
	const string SideSpritePath = "Assets/Art/Tiles/wall_side.png";

	const string FloorTilemapName = "Floor";
	const string WallTilemapName = "Wall";
	const string DecoTilemapName = "Decoration";

	const int FloorSortingOrder = -10;
	const int GroundLevelSortingOrder = 10;

	static readonly (string Keyword, string AssetPath)[] SpriteByTilesetKeyword =
	{
		("noisy", "Assets/Art/Tiles/floor_02_noisy.png"),
		("옆면", "Assets/Art/Tiles/wall_side.png"),
		("윗면", "Assets/Art/Tiles/wall_top_02.png"),
		("벽", "Assets/Art/Tiles/wall_01.png"),
		("wall", "Assets/Art/Tiles/wall_01.png"),
		("바닥", "Assets/Art/Tiles/floor_03.png"),
		("floor", "Assets/Art/Tiles/floor_03.png"),
	};

	[MenuItem("LampLight/Paint Map (Layered)")]
	public static void PaintLayered()
	{
		Paint(true);
	}

	[MenuItem("LampLight/Paint Map (Flat - before)")]
	public static void PaintFlat()
	{
		Paint(false);
	}

	static void Paint(bool layered)
	{
		MapData map = LoadMap();
		if (map == null)
			return;

		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		Tilemap floor = FindTilemap(FloorTilemapName);
		Tilemap wall = FindTilemap(WallTilemapName);
		Tilemap deco = FindTilemap(DecoTilemapName);

		if (floor == null || wall == null || deco == null)
		{
			Debug.LogError($"MapTilemapPainter: need tilemaps named {FloorTilemapName}/{WallTilemapName}/{DecoTilemapName}");
			return;
		}

		Dictionary<int, TileBase> tiles = BuildTileLookup(map);
		if (tiles == null)
			return;

		floor.ClearAllTiles();
		wall.ClearAllTiles();
		deco.ClearAllTiles();

		if (layered)
		{
			PaintLayer(floor, map, map.floor, tiles);
			PaintLayer(wall, map, map.walls, tiles);
			PaintLayer(deco, map, map.deco, tiles);
		}
		else
		{
			PaintLayer(floor, map, map.floor, tiles);
			PaintLayer(floor, map, map.walls, tiles);
			PaintLayer(floor, map, map.deco, tiles);
		}

		ApplyRenderers(floor, wall, deco);
		ApplyWallDepth(wall, layered);

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);

		string mode = layered ? "Layered" : "Flat";
		Debug.Log($"MapTilemapPainter: {mode} 완료 — {map.width}x{map.height}");
	}

	static MapData LoadMap()
	{
		TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>(MapDataPath);
		if (text == null)
		{
			Debug.LogError($"MapTilemapPainter: {MapDataPath} not found");
			return null;
		}

		MapData map = JsonUtility.FromJson<MapData>(text.text);
		if (map == null)
		{
			Debug.LogError($"MapTilemapPainter: failed to parse {MapDataPath}");
			return null;
		}

		string error;
		if (map.Validate(out error) == false)
		{
			Debug.LogWarning($"MapTilemapPainter: MapData incomplete ({error}) — 타일만 그린다");
		}

		return map;
	}

	static Tilemap FindTilemap(string name)
	{
		foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (tilemap.name == name)
				return tilemap;
		}

		return null;
	}

	static Dictionary<int, TileBase> BuildTileLookup(MapData map)
	{
		Directory.CreateDirectory(TileAssetDir);

		HashSet<int> gids = new HashSet<int>();
		CollectGids(map.floor, gids);
		CollectGids(map.walls, gids);
		CollectGids(map.deco, gids);

		Dictionary<int, TileBase> lookup = new Dictionary<int, TileBase>();
		List<int> unresolved = new List<int>();

		foreach (int gid in gids)
		{
			string tilesetName = map.GetTilesetName(gid);
			string spritePath = ResolveSpritePath(tilesetName);

			if (spritePath == null)
			{
				unresolved.Add(gid);
				continue;
			}

			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
			if (sprite == null)
			{
				Debug.LogError($"MapTilemapPainter: sprite not found at {spritePath}");
				return null;
			}

			lookup[gid] = GetOrCreateTile(sprite);
		}

		if (unresolved.Count > 0)
		{
			Debug.LogError(
				$"MapTilemapPainter: 스프라이트를 못 찾은 gid {unresolved.Count}개 — {string.Join(", ", unresolved)}. " +
				"MapData.json의 tilesets 이름과 SpriteByTilesetKeyword를 맞춰야 한다");
			return null;
		}

		return lookup;
	}

	static void CollectGids(int[] layer, HashSet<int> into)
	{
		if (layer == null)
			return;

		foreach (int gid in layer)
		{
			if (gid != 0)
				into.Add(gid);
		}
	}

	static string ResolveSpritePath(string tilesetName)
	{
		if (string.IsNullOrEmpty(tilesetName))
			return null;

		string lowered = tilesetName.ToLowerInvariant();
		foreach ((string keyword, string assetPath) in SpriteByTilesetKeyword)
		{
			if (lowered.Contains(keyword))
				return assetPath;
		}

		return null;
	}

	static TileBase GetOrCreateTile(Sprite sprite)
	{
		string path = $"{TileAssetDir}/{sprite.name}.asset";

		Tile existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
		if (existing != null)
		{
			existing.sprite = sprite;
			EditorUtility.SetDirty(existing);
			return existing;
		}

		Tile tile = ScriptableObject.CreateInstance<Tile>();
		tile.sprite = sprite;
		tile.colliderType = Tile.ColliderType.Grid;
		AssetDatabase.CreateAsset(tile, path);
		return tile;
	}

	static readonly string[] SideVariantPaths =
	{
		"Assets/Art/Tiles/wall_side_a.png",
		"Assets/Art/Tiles/wall_side_b.png",
		"Assets/Art/Tiles/wall_side_c.png",
	};

	static TileBase[] LoadSideVariants()
	{
		TileBase[] variants = new TileBase[SideVariantPaths.Length];
		for (int i = 0; i < SideVariantPaths.Length; i++)
		{
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SideVariantPaths[i]);
			if (sprite == null)
				return null;

			variants[i] = GetOrCreateTile(sprite);
		}

		return variants;
	}

	static bool IsSideCell(MapData map, int col, int row)
	{
		if (col < 0 || col >= map.width || row < 0 || row >= map.height)
			return false;

		int gid = map.walls[row * map.width + col];
		return gid != 0 && ResolveSpritePath(map.GetTilesetName(gid)) == SideSpritePath;
	}

	static TileBase PickSideVariant(MapData map, int col, int row, TileBase[] variants)
	{
		int above = 0;
		while (IsSideCell(map, col, row - 1 - above))
			above++;

		int below = 0;
		while (IsSideCell(map, col, row + 1 + below))
			below++;

		int runLength = above + below + 1;
		if (runLength <= 1)
			return variants[0];

		if (above == 0)
			return variants[0];

		if (below == 0)
			return variants[2];

		return variants[1];
	}

	static void PaintLayer(Tilemap tilemap, MapData map, int[] layer, Dictionary<int, TileBase> tiles)
	{
		PaintLayer(tilemap, map, layer, tiles, null);
	}

	static void PaintLayer(Tilemap tilemap, MapData map, int[] layer, Dictionary<int, TileBase> tiles,
		TileBase[] sideVariants)
	{
		if (layer == null)
			return;

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				int gid = layer[row * map.width + col];
				if (gid == 0)
					continue;

				TileBase tile;
				if (tiles.TryGetValue(gid, out tile) == false)
					continue;

				if (sideVariants != null && ResolveSpritePath(map.GetTilesetName(gid)) == SideSpritePath)
					tile = PickSideVariant(map, col, row, sideVariants);

				tilemap.SetTile(new Vector3Int(col, map.height - 1 - row, 0), tile);
			}
		}
	}

	static void ApplyRenderers(Tilemap floor, Tilemap wall, Tilemap deco)
	{
		SetRenderer(floor, FloorSortingOrder);
		SetRenderer(wall, GroundLevelSortingOrder);
		SetRenderer(deco, GroundLevelSortingOrder);
	}

	static void SetRenderer(Tilemap tilemap, int sortingOrder)
	{
		TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
		if (renderer == null)
			return;

		renderer.mode = TilemapRenderer.Mode.Individual;
		renderer.sortingOrder = sortingOrder;
		EditorUtility.SetDirty(renderer);
	}

	[MenuItem("LampLight/Merge Wall Colliders")]
	public static void MergeWallColliders()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		Tilemap wall = FindTilemap(WallTilemapName);
		if (wall == null)
		{
			Debug.LogError($"MapTilemapPainter: {WallTilemapName} 타일맵 없음");
			return;
		}

		ApplyWallComposite(wall);

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		Debug.Log("MapTilemapPainter: wall composite collider 적용");
	}

	static void ApplyWallComposite(Tilemap wall)
	{
		TilemapCollider2D collider = wall.GetComponent<TilemapCollider2D>();
		if (collider == null)
			collider = wall.gameObject.AddComponent<TilemapCollider2D>();

		Rigidbody2D body = wall.GetComponent<Rigidbody2D>();
		if (body == null)
			body = wall.gameObject.AddComponent<Rigidbody2D>();

		body.bodyType = RigidbodyType2D.Static;
		EditorUtility.SetDirty(body);

		CompositeCollider2D composite = wall.GetComponent<CompositeCollider2D>();
		if (composite == null)
			composite = wall.gameObject.AddComponent<CompositeCollider2D>();

		composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
		composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
		EditorUtility.SetDirty(composite);

		collider.compositeOperation = Collider2D.CompositeOperation.Merge;
		EditorUtility.SetDirty(collider);
	}

	static void ApplyWallDepth(Tilemap wall, bool enabled)
	{
		TilemapCollider2D collider = wall.GetComponent<TilemapCollider2D>();
		if (collider == null)
			collider = wall.gameObject.AddComponent<TilemapCollider2D>();

		collider.enabled = enabled;
		EditorUtility.SetDirty(collider);

		if (enabled)
			ApplyWallComposite(wall);

		ShadowCaster2D existing = wall.GetComponent<ShadowCaster2D>();
		if (existing != null)
			Object.DestroyImmediate(existing);

		ShadowCaster2D caster = wall.gameObject.AddComponent<ShadowCaster2D>();

		SerializedObject so = new SerializedObject(caster);
		SerializedProperty option = so.FindProperty("m_CastingOption");
		if (option != null)
		{
			option.intValue = enabled
				? (int)ShadowCaster2D.ShadowCastingOptions.CastShadow
				: (int)ShadowCaster2D.ShadowCastingOptions.NoShadow;
			so.ApplyModifiedPropertiesWithoutUndo();
		}
		else
		{
			Debug.LogWarning("MapTilemapPainter: m_CastingOption 없음 (URP 버전 차이)");
		}

		caster.enabled = enabled;
		EditorUtility.SetDirty(caster);

		SerializedObject check = new SerializedObject(caster);
		SerializedProperty source = check.FindProperty("m_ShadowCastingSource");
		SerializedProperty shape = check.FindProperty("m_ShadowShape2DComponent");
		Debug.Log($"MapTilemapPainter: wall shadows {(enabled ? "on" : "off")} — " +
			$"source={(source != null ? source.intValue.ToString() : "?")} " +
			$"shape={(shape != null && shape.objectReferenceValue != null ? shape.objectReferenceValue.GetType().Name : "없음")}");
	}
}
