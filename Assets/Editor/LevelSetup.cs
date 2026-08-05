using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelSetup
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string SourcePrefab = "Assets/Resources/Prefabs/WandererZombie.prefab";
	const string RunnerPrefab = "Assets/Resources/Prefabs/RunnerZombie.prefab";
	const string SoundDir = "Assets/Resources/Audio";
	const string SpawnerName = "@EnemySpawner";

	[MenuItem("LampLight/Level/Setup All")]
	public static void SetupAll()
	{
		BuildRunnerPrefab();
		WireScene();
	}

	[MenuItem("LampLight/Level/Build Runner Prefab")]
	public static void BuildRunnerPrefab()
	{
		GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
		if (source == null)
		{
			Debug.LogError($"LevelSetup: missing {SourcePrefab}");
			return;
		}

		if (AssetDatabase.CopyAsset(SourcePrefab, RunnerPrefab) == false)
		{
			Debug.LogError($"LevelSetup: failed to copy into {RunnerPrefab}");
			return;
		}

		AssetDatabase.ImportAsset(RunnerPrefab);
		GameObject root = PrefabUtility.LoadPrefabContents(RunnerPrefab);

		try
		{
			WandererZombie legacy = root.GetComponent<WandererZombie>();
			if (legacy != null)
				Object.DestroyImmediate(legacy, true);

			EnemyFootstep inherited = root.GetComponent<EnemyFootstep>();
			if (inherited != null)
				Object.DestroyImmediate(inherited, true);

			RunnerZombie runner = Util.GetOrAddComponent<RunnerZombie>(root);
			OcclusionSource alert = Util.GetOrAddComponent<OcclusionSource>(root);

			AudioClip clear = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundDir}/runner_hit.wav");
			AudioClip muffled = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundDir}/runner_hit_muffled.wav");

			SerializedObject so = new SerializedObject(alert);
			so.FindProperty("_clearClip").objectReferenceValue = clear;
			so.FindProperty("_muffledClip").objectReferenceValue = muffled;
			so.FindProperty("_bus").enumValueIndex = (int)Define.Sound.Threat;
			so.FindProperty("_radiusTiles").floatValue = 12.0f;
			so.ApplyModifiedPropertiesWithoutUndo();

			SerializedObject sr = new SerializedObject(runner);
			sr.FindProperty("_alert").objectReferenceValue = alert;
			sr.ApplyModifiedPropertiesWithoutUndo();

			PrefabUtility.SaveAsPrefabAsset(root, RunnerPrefab);
			Debug.Log($"LevelSetup: {RunnerPrefab} built (alert clip {(clear == null ? "<none>" : clear.name)})");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
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

		SoundRing ring = Object.FindFirstObjectByType<SoundRing>();
		if (ring == null)
		{
			GameObject go = new GameObject("@SoundRing");
			Util.GetOrAddComponent<SoundRing>(go);
			Debug.Log("LevelSetup: added @SoundRing");
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
