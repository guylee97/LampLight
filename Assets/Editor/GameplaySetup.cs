using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class GameplaySetup
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string SceneUIDir = "Assets/Resources/Prefabs/UI/Scene";
	const string PopupUIDir = "Assets/Resources/Prefabs/UI/Popup";
	const string MapRootName = "@Map";
	const string GameRootName = "@Game";
	const string DressedRootName = "@Dressing";
	const string PlayerPrefabPath = "Assets/Resources/Prefabs/Player.prefab";

	const int EnemyLayer = 8;
	const int PlayerLayer = 9;
	const int BlockLayer = 10;

	const string WallTilemapName = "Wall";

	static readonly Color WallFlatTint = new Color(0.152f, 0.152f, 0.152f, 1.0f);

	static readonly Color Ink = new Color(0.90f, 0.86f, 0.76f, 1.0f);
	static readonly Color Dim = new Color(0.04f, 0.04f, 0.05f, 0.82f);
	static readonly Color Panel = new Color(0.09f, 0.09f, 0.11f, 0.94f);
	static readonly Color Track = new Color(0.14f, 0.13f, 0.12f, 0.75f);
	static readonly Color Flame = new Color(0.98f, 0.72f, 0.32f, 1.0f);
	static readonly Color Breath = new Color(0.46f, 0.78f, 0.92f, 1.0f);

	[MenuItem("LampLight/Set Up Gameplay")]
	public static void Run()
	{
		ConfigurePhysics();
		BuildUIPrefabs();
		WireScene();

		AssetDatabase.SaveAssets();
		Debug.Log("GameplaySetup: done");
	}

	static void ConfigurePhysics()
	{
		Physics2D.IgnoreLayerCollision(EnemyLayer, EnemyLayer, true);
		Physics2D.queriesHitTriggers = true;
		Debug.Log("GameplaySetup: enemy-enemy collisions disabled");
	}

	static void BuildUIPrefabs()
	{
		Directory.CreateDirectory(SceneUIDir);
		Directory.CreateDirectory(PopupUIDir);

		BuildHud();
		BuildResultPopup();
		BuildPausePopup();
	}

	static Font DefaultFont()
	{
		Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		if (font == null)
			font = Resources.GetBuiltinResource<Font>("Arial.ttf");

		return font;
	}

	static GameObject NewCanvasRoot(string name)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));

		Canvas canvas = go.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;

		CanvasScaler scaler = go.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);
		scaler.matchWidthOrHeight = 0.5f;

		go.AddComponent<GraphicRaycaster>();
		return go;
	}

	static RectTransform Child(string name, Transform parent)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go.GetComponent<RectTransform>();
	}

	static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
	{
		rect.anchorMin = anchor;
		rect.anchorMax = anchor;
		rect.pivot = pivot;
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	static Text MakeText(string name, Transform parent, int size, TextAnchor anchor, Color color)
	{
		RectTransform rect = Child(name, parent);
		Text text = rect.gameObject.AddComponent<Text>();
		text.font = DefaultFont();
		text.fontSize = size;
		text.alignment = anchor;
		text.color = color;
		text.horizontalOverflow = HorizontalWrapMode.Overflow;
		text.verticalOverflow = VerticalWrapMode.Overflow;
		text.raycastTarget = false;
		return text;
	}

	static Image MakeImage(string name, Transform parent, Color color)
	{
		RectTransform rect = Child(name, parent);
		Image image = rect.gameObject.AddComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
		return image;
	}

	static Image MakeBar(string name, Transform parent, Color fillColor, Vector2 position, Vector2 size)
	{
		Image track = MakeImage(name + "Track", parent, Track);
		Place(track.rectTransform, new Vector2(0, 0), new Vector2(0, 0), position, size);

		Image fill = MakeImage(name, track.transform, fillColor);
		Stretch(fill.rectTransform);
		fill.type = Image.Type.Filled;
		fill.fillMethod = Image.FillMethod.Horizontal;
		fill.fillOrigin = (int)Image.OriginHorizontal.Left;
		fill.fillAmount = 1.0f;

		return fill;
	}

	static Button MakeButton(string name, string label, Transform parent, Vector2 position, Vector2 size)
	{
		RectTransform rect = Child(name, parent);
		Image background = rect.gameObject.AddComponent<Image>();
		background.color = new Color(0.20f, 0.19f, 0.22f, 1.0f);

		Button button = rect.gameObject.AddComponent<Button>();
		button.targetGraphic = background;

		Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

		Text text = MakeText(name + "Label", rect, 32, TextAnchor.MiddleCenter, Ink);
		text.text = label;
		Stretch(text.rectTransform);

		return button;
	}

	static void Save(GameObject go, string dir, string name)
	{
		string path = $"{dir}/{name}.prefab";
		PrefabUtility.SaveAsPrefabAsset(go, path);
		Object.DestroyImmediate(go);
		Debug.Log($"GameplaySetup: {path}");
	}

	static void BuildHud()
	{
		GameObject root = NewCanvasRoot("UI_InGame");
		root.AddComponent<UI_InGame>();

		Text artifacts = MakeText("ArtifactText", root.transform, 44, TextAnchor.UpperLeft, Ink);
		artifacts.text = "공양물  0 / 4";
		Place(artifacts.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -40), new Vector2(420, 60));

		Image fuel = MakeBar("FuelFill", root.transform, Flame, new Vector2(48, 118), new Vector2(360, 26));
		Image stamina = MakeBar("StaminaFill", root.transform, Breath, new Vector2(48, 74), new Vector2(360, 18));

		Text fuelText = MakeText("FuelText", root.transform, 28, TextAnchor.LowerLeft, Ink);
		fuelText.text = "등불  90s";
		Place(fuelText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(48, 154), new Vector2(360, 40));

		Text prompt = MakeText("PromptText", root.transform, 36, TextAnchor.LowerCenter, Ink);
		prompt.text = string.Empty;
		Place(prompt.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(900, 50));

		if (fuel == null || stamina == null)
			Debug.LogError("GameplaySetup: failed to build HUD bars");

		Save(root, SceneUIDir, "UI_InGame");
	}

	static void BuildResultPopup()
	{
		GameObject root = NewCanvasRoot("UI_Result");
		root.AddComponent<UI_Result>();

		Image dim = MakeImage("Dim", root.transform, Dim);
		Stretch(dim.rectTransform);
		dim.raycastTarget = true;

		Image panel = MakeImage("Panel", root.transform, Panel);
		Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 480));

		Text title = MakeText("ResultTitleText", panel.transform, 68, TextAnchor.MiddleCenter, Ink);
		title.text = "탈출 성공";
		Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(700, 90));

		Text detail = MakeText("ResultDetailText", panel.transform, 36, TextAnchor.MiddleCenter, Ink);
		detail.text = "공양물  0 / 4";
		Place(detail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(700, 60));

		MakeButton("RetryButton", "다시 시도", panel.transform, new Vector2(0, -60), new Vector2(400, 80));
		MakeButton("TitleButton", "타이틀로", panel.transform, new Vector2(0, -160), new Vector2(400, 80));

		Save(root, PopupUIDir, "UI_Result");
	}

	static void BuildPausePopup()
	{
		GameObject root = NewCanvasRoot("UI_Pause");
		root.AddComponent<UI_Pause>();

		Image dim = MakeImage("Dim", root.transform, Dim);
		Stretch(dim.rectTransform);
		dim.raycastTarget = true;

		Image panel = MakeImage("Panel", root.transform, Panel);
		Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660, 400));

		Text title = MakeText("PauseTitleText", panel.transform, 56, TextAnchor.MiddleCenter, Ink);
		title.text = "일시정지";
		Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(600, 80));

		MakeButton("ResumeButton", "계속하기", panel.transform, new Vector2(0, 10), new Vector2(400, 80));
		MakeButton("TitleButton", "타이틀로", panel.transform, new Vector2(0, -90), new Vector2(400, 80));

		Save(root, PopupUIDir, "UI_Pause");
	}

	static void WireScene()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		GameObject mapRoot = FindRoot(MapRootName);
		if (mapRoot == null)
		{
			Debug.LogError($"GameplaySetup: {MapRootName} not found — run LampLight/Set Up Map Scene first");
			return;
		}

		PlayerController player = EnsurePlayerFromPrefab();
		if (player == null)
		{
			Debug.LogError("GameplaySetup: no PlayerController in the scene");
			return;
		}

		WirePlayer(player);
		WireEnemies();
		FlattenWallLighting();
		WireGameRoot(mapRoot, player);

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		Debug.Log("GameplaySetup: scene wired");
	}

	static void FlattenWallLighting()
	{
		Tilemap wall = null;
		foreach (Tilemap map in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (map.name == WallTilemapName)
			{
				wall = map;
				break;
			}
		}

		if (wall == null)
		{
			Debug.LogWarning($"GameplaySetup: no tilemap named {WallTilemapName}");
			return;
		}

		TilemapRenderer renderer = wall.GetComponent<TilemapRenderer>();
		if (renderer != null)
		{
			renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
			EditorUtility.SetDirty(renderer);
		}

		wall.color = WallFlatTint;
		EditorUtility.SetDirty(wall);

		Debug.Log($"GameplaySetup: {WallTilemapName} tilemap is unlit, tint {WallFlatTint}");
	}

	static GameObject FindRoot(string name)
	{
		foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (go.name == name && go.transform.parent == null)
				return go;
		}

		return null;
	}

	static PlayerController EnsurePlayerFromPrefab()
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
		PlayerController existing = Object.FindFirstObjectByType<PlayerController>();

		if (prefab == null)
		{
			Debug.LogWarning($"GameplaySetup: {PlayerPrefabPath} is missing");
			return existing;
		}

		if (existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject) == prefab)
			return existing;

		Vector3 position = existing != null ? existing.transform.position : Vector3.zero;

		if (existing != null)
		{
			Debug.Log($"GameplaySetup: replacing scene player '{existing.name}' with {PlayerPrefabPath}");
			Object.DestroyImmediate(existing.gameObject);
		}

		GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
		spawned.transform.position = position;
		return spawned.GetComponent<PlayerController>();
	}

	static void WirePlayer(PlayerController player)
	{
		player.gameObject.layer = PlayerLayer;

		Lamp lamp = player.GetComponentInChildren<Lamp>();

		SerializedObject playerSo = new SerializedObject(player);
		playerSo.FindProperty("_lamp").objectReferenceValue = lamp;
		playerSo.ApplyModifiedPropertiesWithoutUndo();

		if (lamp != null)
		{
			SerializedObject lampSo = new SerializedObject(lamp);
			lampSo.FindProperty("_reactiveMask").intValue = 1 << EnemyLayer;
			lampSo.FindProperty("_obstacleMask").intValue = 1 << BlockLayer;
			lampSo.ApplyModifiedPropertiesWithoutUndo();
		}

		PlayerInteractor interactor = Util.GetOrAddComponent<PlayerInteractor>(player.gameObject);
		SerializedObject interactorSo = new SerializedObject(interactor);
		interactorSo.FindProperty("_player").objectReferenceValue = player;
		interactorSo.FindProperty("_mask").intValue = ~(1 << BlockLayer);
		interactorSo.ApplyModifiedPropertiesWithoutUndo();

		EditorUtility.SetDirty(player.gameObject);
	}

	static void WireEnemies()
	{
		int count = 0;

		foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			enemy.gameObject.layer = EnemyLayer;
			EditorUtility.SetDirty(enemy.gameObject);
			count++;
		}

		Debug.Log($"GameplaySetup: {count} enemies wired");
	}

	static void WireGameRoot(GameObject mapRoot, PlayerController player)
	{
		GameObject root = FindRoot(GameRootName);
		if (root == null)
			root = new GameObject(GameRootName);

		InGameScene scene = Util.GetOrAddComponent<InGameScene>(root);

		SerializedObject so = new SerializedObject(scene);
		so.FindProperty("_progress").objectReferenceValue = mapRoot.GetComponent<StageProgress>();
		so.FindProperty("_placer").objectReferenceValue = mapRoot.GetComponent<MapObjectPlacer>();
		so.FindProperty("_selector").objectReferenceValue = mapRoot.GetComponent<SpawnSelector>();
		so.FindProperty("_player").objectReferenceValue = player;

		CameraController camera = Object.FindFirstObjectByType<CameraController>();
		so.FindProperty("_camera").objectReferenceValue = camera;
		so.ApplyModifiedPropertiesWithoutUndo();

		if (camera != null)
		{
			SerializedObject cameraSo = new SerializedObject(camera);
			cameraSo.FindProperty("_target").objectReferenceValue = player.transform;
			cameraSo.ApplyModifiedPropertiesWithoutUndo();
		}

		EditorUtility.SetDirty(root);
	}
}
