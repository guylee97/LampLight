using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapSceneSetup
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string MapRootName = "@Map";
	const string ManagersObjectName = "@Managers";
	const string ObjectArtDir = "Assets/Resources/Art/Objects/artifact";

	static readonly string[] ArtifactSpriteNames =
	{
		"obj_artifact_bell",
		"obj_artifact_crest",
		"obj_artifact_mask",
		"obj_artifact_seal"
	};

	[MenuItem("LampLight/Set Up Map Scene")]
	public static void Run()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		FixGrid();
		FixCamera();
		FixManagersName();
		BuildMapRoot();

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		Debug.Log("MapSceneSetup: done");
	}

	static T FindInScene<T>() where T : Component
	{
		T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		return found.Length > 0 ? found[0] : null;
	}

	static void FixGrid()
	{
		Grid grid = FindInScene<Grid>();
		if (grid == null)
		{
			Debug.LogError("MapSceneSetup: no Grid in scene");
			return;
		}

		grid.cellSize = new Vector3(1, 1, 0);
		EditorUtility.SetDirty(grid);
		Debug.Log($"MapSceneSetup: grid cellSize -> {grid.cellSize}");
	}

	static void FixCamera()
	{
		Camera camera = Camera.main != null ? Camera.main : FindInScene<Camera>();
		if (camera == null)
		{
			Debug.LogError("MapSceneSetup: no Camera in scene");
			return;
		}

		camera.orthographic = true;
		camera.orthographicSize = 8.4375f;
		EditorUtility.SetDirty(camera);
		Debug.Log($"MapSceneSetup: camera orthographicSize -> {camera.orthographicSize}");
	}

	static void FixManagersName()
	{
		Managers managers = FindInScene<Managers>();
		if (managers == null)
			return;

		if (managers.name == ManagersObjectName)
			return;

		Debug.LogWarning($"MapSceneSetup: renaming '{managers.name}' -> '{ManagersObjectName}' so the singleton is found");
		managers.name = ManagersObjectName;
		EditorUtility.SetDirty(managers.gameObject);
	}

	static void BuildMapRoot()
	{
		GameObject root = null;
		foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (go.name == MapRootName && go.transform.parent == null)
			{
				root = go;
				break;
			}
		}

		if (root == null)
			root = new GameObject(MapRootName);

		StageProgress progress = root.GetComponent<StageProgress>();
		if (progress == null)
			progress = root.AddComponent<StageProgress>();

		MapObjectPlacer placer = root.GetComponent<MapObjectPlacer>();
		if (placer == null)
			placer = root.AddComponent<MapObjectPlacer>();

		SpawnSelector selector = root.GetComponent<SpawnSelector>();
		if (selector == null)
			selector = root.AddComponent<SpawnSelector>();

		SerializedObject placerSo = new SerializedObject(placer);
		placerSo.FindProperty("_progress").objectReferenceValue = progress;
		placerSo.FindProperty("_root").objectReferenceValue = root.transform;

		SerializedProperty sprites = placerSo.FindProperty("_artifactSprites");
		sprites.arraySize = 4;
		for (int i = 0; i < 4; i++)
		{
			string path = $"{ObjectArtDir}/{ArtifactSpriteNames[i]}.png";
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
			if (sprite == null)
				Debug.LogError($"MapSceneSetup: sprite not found at {path}");
			sprites.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
		}
		placerSo.ApplyModifiedPropertiesWithoutUndo();

		SerializedObject selectorSo = new SerializedObject(selector);
		selectorSo.FindProperty("_placer").objectReferenceValue = placer;
		selectorSo.ApplyModifiedPropertiesWithoutUndo();

		EditorUtility.SetDirty(root);
		Debug.Log($"MapSceneSetup: {MapRootName} ready with StageProgress, MapObjectPlacer, SpawnSelector");
	}
}
