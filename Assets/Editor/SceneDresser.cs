using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneDresser
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string PropArtDir = "Assets/Resources/Art/Objects/prop";
	const string PropPrefabDir = "Assets/Resources/Prefabs/Props";

	const string DressedRootName = "@Dressing";

	const int CharacterSortingOrder = 20;
	const int PropSortingOrder = 5;

	const int Seed = 20260728;
	const int PropCount = 26;


	static readonly string[] PropNames =
	{
		"prop_oil_lamp", "prop_broken_cup", "prop_bone", "prop_seal_stone",
	};

	[MenuItem("LampLight/Dress Scene With Assets")]
	public static void Run()
	{
		Directory.CreateDirectory(PropPrefabDir);

		List<GameObject> props = BuildPropPrefabs();

		if (props.Count == 0)
			return;

		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		MapData map = LoadMap();
		if (map == null)
			return;

		Transform root = MakeRoot();
		List<Vector2Int> floor = WalkableCells(map);
		System.Random rng = new System.Random(Seed);

		PlacePlayerAtStart(map);
		RemoveLegacyEnemies();

		PlaceProps(map, root, props, floor, rng);

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		Debug.Log($"SceneDresser: done — 소품 {PropCount}, 바닥칸 {floor.Count}");
	}


	static List<GameObject> BuildPropPrefabs()
	{
		List<GameObject> made = new List<GameObject>();

		foreach (string name in PropNames)
		{
			string spritePath = $"{PropArtDir}/{name}.png";
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
			if (sprite == null)
			{
				Debug.LogError($"SceneDresser: sprite not found at {spritePath}");
				continue;
			}

			GameObject go = new GameObject(name);
			SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
			renderer.sprite = sprite;
			renderer.sortingOrder = PropSortingOrder;


			string prefabPath = $"{PropPrefabDir}/{name}.prefab";
			made.Add(PrefabUtility.SaveAsPrefabAsset(go, prefabPath));
			Object.DestroyImmediate(go);
		}

		Debug.Log($"SceneDresser: 소품 프리팹 {made.Count}종");
		return made;
	}


	static Transform MakeRoot()
	{
		foreach (GameObject go in Object.FindObjectsByType<GameObject>(
			FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (go.name == DressedRootName && go.transform.parent == null)
			{
				Object.DestroyImmediate(go);
				break;
			}
		}

		return new GameObject(DressedRootName).transform;
	}

	static List<Vector2Int> WalkableCells(MapData map)
	{
		List<Vector2Int> cells = new List<Vector2Int>();
		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (map.walls[row * map.width + col] == 0)
					cells.Add(new Vector2Int(col, row));
			}
		}

		return cells;
	}

	static Vector3 ToWorld(MapData map, Vector2Int cell)
	{
		return new Vector3(cell.x + 0.5f, (map.height - 1 - cell.y) + 0.5f, 0);
	}

	static bool TouchesWall(MapData map, Vector2Int cell)
	{
		for (int dy = -1; dy <= 1; dy++)
		{
			for (int dx = -1; dx <= 1; dx++)
			{
				int c = cell.x + dx;
				int r = cell.y + dy;
				if (c < 0 || c >= map.width || r < 0 || r >= map.height)
					continue;

				if (map.walls[r * map.width + c] != 0)
					return true;
			}
		}

		return false;
	}

	static void PlacePlayerAtStart(MapData map)
	{
		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		if (player == null)
			return;

		MapPoint start = null;
		if (map.objects != null)
		{
			foreach (MapPoint point in map.objects)
			{
				if (point.name == "player_start")
				{
					start = point;
					break;
				}
			}
		}

		if (start == null)
		{
			Debug.LogWarning("SceneDresser: player_start 없음 — 플레이어를 그대로 둔다");
			return;
		}

		Vector3 world = new Vector3(start.col + 0.5f, (map.height - 1 - start.row) + 0.5f, 0);
		player.transform.position = world;

		Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
		if (camera != null)
			camera.transform.position = new Vector3(world.x, world.y, camera.transform.position.z);

		EditorUtility.SetDirty(player);
		Debug.Log($"SceneDresser: player_start ({start.col},{start.row}) -> {world}");
	}

	static void RemoveLegacyEnemies()
	{
		int removed = 0;
		foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>(
			FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (enemy == null)
				continue;

			if (enemy.transform.parent != null && enemy.transform.parent.name == DressedRootName)
				continue;

			Debug.Log($"SceneDresser: 옛 적 오브젝트 제거 — {enemy.name}");
			Object.DestroyImmediate(enemy.gameObject);
			removed++;
		}

		if (removed > 0)
			Debug.Log($"SceneDresser: 손으로 놓였던 적 {removed}개 정리");
	}

	static void PlaceProps(MapData map, Transform root, List<GameObject> props,
		List<Vector2Int> floor, System.Random rng)
	{
		List<Vector2Int> nearWall = floor.FindAll(c => TouchesWall(map, c));
		if (nearWall.Count == 0)
			nearWall = floor;

		HashSet<Vector2Int> used = new HashSet<Vector2Int>();
		int placed = 0;
		int guard = 0;

		while (placed < PropCount && guard++ < PropCount * 40)
		{
			Vector2Int cell = nearWall[rng.Next(nearWall.Count)];
			if (used.Contains(cell))
				continue;

			used.Add(cell);

			GameObject prefab = props[rng.Next(props.Count)];
			GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);

			Vector3 jitter = new Vector3((float)rng.NextDouble() * 0.5f - 0.25f,
										 (float)rng.NextDouble() * 0.4f - 0.2f, 0);
			instance.transform.position = ToWorld(map, cell) + jitter;
			placed++;
		}

		Debug.Log($"SceneDresser: 소품 {placed}개 배치 (벽 접한 칸 {nearWall.Count}곳 중)");
	}

	static MapData LoadMap()
	{
		TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Data/MapData.json");
		if (text == null)
		{
			Debug.LogError("SceneDresser: MapData.json not found");
			return null;
		}

		return JsonUtility.FromJson<MapData>(text.text);
	}
}
