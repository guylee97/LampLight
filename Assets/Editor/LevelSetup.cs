using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelSetup
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string SoundDir = "Assets/Resources/Audio";
	const string SpawnerName = "@EnemySpawner";

	[MenuItem("LampLight/Level/Setup All")]
	public static void SetupAll()
	{
		WireScene();
	}

	[MenuItem("LampLight/Level/Wire Scene")]
	public static void WireScene()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
		if (spawner == null)
		{
			GameObject go = GameObject.Find(SpawnerName);
			if (go == null)
				go = new GameObject(SpawnerName);

			spawner = Util.GetOrAddComponent<EnemySpawner>(go);
			Debug.Log($"LevelSetup: added {SpawnerName}");
		}

		InGameScene inGame = Object.FindFirstObjectByType<InGameScene>();
		if (inGame != null)
		{
			SerializedObject so = new SerializedObject(inGame);
			so.FindProperty("_spawner").objectReferenceValue = spawner;
			so.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(inGame);
		}
		else
		{
			Debug.LogError("LevelSetup: no InGameScene in scene");
		}

		MapTilemapRenderer tilemap = Object.FindFirstObjectByType<MapTilemapRenderer>();
		if (tilemap == null)
		{
			GameObject go = new GameObject("@MapTilemap");
			tilemap = Util.GetOrAddComponent<MapTilemapRenderer>(go);
			Debug.Log("LevelSetup: added @MapTilemap");
		}

		if (inGame != null)
		{
			SerializedObject st = new SerializedObject(inGame);
			st.FindProperty("_tilemap").objectReferenceValue = tilemap;
			st.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(inGame);
		}

		TutorialController tutorial = Object.FindFirstObjectByType<TutorialController>();
		if (tutorial == null)
		{
			GameObject go = new GameObject("@Tutorial");
			Util.GetOrAddComponent<TutorialController>(go);
			Debug.Log("LevelSetup: added @Tutorial");
		}

		AmbienceController ambience = Object.FindFirstObjectByType<AmbienceController>();
		if (ambience == null)
		{
			GameObject go = new GameObject("@Ambience");
			Util.GetOrAddComponent<AmbienceController>(go);
			Debug.Log("LevelSetup: added @Ambience");
		}

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		Debug.Log("LevelSetup: scene wired");
	}
}
