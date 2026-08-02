using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AudioSetup
{
	const string SoundDir = "Assets/Resources/Sounds";
	const string ArtifactPrefab = "Assets/Resources/Prefabs/Map/Artifact.prefab";
	const string ExitDoorPrefab = "Assets/Resources/Prefabs/Map/ExitDoor.prefab";
	const string BuildDir = "Build/WebGL";

	static readonly string[] Scenes =
	{
		"Assets/Scenes/Title.unity",
		"Assets/Scenes/InGame.unity",
	};

	const string PlayerPrefab = "Assets/Resources/Prefabs/Player.prefab";
	const string WalkerPrefab = "Assets/Resources/Prefabs/DefaultZombie.prefab";
	const string WandererPrefab = "Assets/Resources/Prefabs/ActiveZombie.prefab";
	const string RunnerPrefab = "Assets/Resources/Prefabs/RunnerZombie.prefab";
	const string InGameScenePath = "Assets/Scenes/InGame.unity";

	[MenuItem("LampLight/Audio/Setup All")]
	public static void SetupAll()
	{
		ConfigureWebGL();
		FixClipImport();
		FixListenerRig();
		WirePrefabs();
		WireGameplayClips();
		Verify();
	}

	[MenuItem("LampLight/Audio/Fix Clip Import")]
	public static void FixClipImport()
	{
		string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SoundDir });
		int changed = 0;

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
			if (importer == null)
				continue;

			AudioImporterSampleSettings settings = importer.defaultSampleSettings;

			bool dirty = settings.preloadAudioData == false
				|| settings.loadType != AudioClipLoadType.DecompressOnLoad
				|| importer.loadInBackground;

			settings.preloadAudioData = true;
			settings.loadType = AudioClipLoadType.DecompressOnLoad;

			importer.defaultSampleSettings = settings;
			importer.loadInBackground = false;

			if (dirty == false)
				continue;

			importer.SaveAndReimport();
			changed++;
		}

		Debug.Log($"AudioSetup: clip import fixed ({changed}/{guids.Length} reimported, preload on)");
	}

	[MenuItem("LampLight/Audio/Fix Listener Rig")]
	public static void FixListenerRig()
	{
		UnityEngine.SceneManagement.Scene scene =
			UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
				"Assets/Scenes/InGame.unity",
				UnityEditor.SceneManagement.OpenSceneMode.Single);

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		if (player == null)
		{
			Debug.LogError("AudioSetup: no PlayerController in the scene");
			return;
		}

		int removed = 0;
		foreach (AudioListener listener in Object.FindObjectsByType<AudioListener>(
			FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (listener.gameObject == player.gameObject)
				continue;

			Object.DestroyImmediate(listener, true);
			removed++;
		}

		Util.GetOrAddComponent<AudioListener>(player.gameObject);

		UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
		UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

		Debug.Log($"AudioSetup: listener moved onto {player.name} ({removed} removed from elsewhere)");
	}

	[MenuItem("LampLight/Audio/Wire Gameplay Clips")]
	public static void WireGameplayClips()
	{
		WirePlayer();
		WireEnemyFootstep(WalkerPrefab, "walker_step", 0.90f, 0.0f, 8.0f, "walker_breath", 6.0f);
		WireEnemyFootstep(WandererPrefab, "wanderer_step", 0.55f, 0.15f, 9.0f, "wanderer_alert", 6.0f);
		WireEnemyFootstep(RunnerPrefab, "runner_hit", 0.40f, 0.10f, 10.0f, "runner_pass", 7.0f);
		WireStateSounds(WalkerPrefab, "walker_breath", "walker_breath", "wanderer_alert");
		WireStateSounds(WandererPrefab, "wanderer_step", "wanderer_step", "wanderer_alert");
		WireAmbience();
		WireDrone(ArtifactPrefab, "artifact_drone", 6.0f);
		WireDrone(ExitDoorPrefab, "exit_hum", 7.0f);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("AudioSetup: gameplay clips wired");
	}

	static void WirePlayer()
	{
		GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);

		try
		{
			PlayerController player = root.GetComponent<PlayerController>();
			if (player == null)
			{
				Debug.LogError("AudioSetup: Player.prefab has no PlayerController");
				return;
			}

			SerializedObject so = new SerializedObject(player);
			SetClipArray(so, "_walkFootstepClips", "step_walk");
			SetClipArray(so, "_sneakFootstepClips", "step_sneak");
			SetClipArray(so, "_runFootstepClips", "step_run");
			SetClipArray(so, "_noisyFootstepClips", "step_noisy_floor");
			so.ApplyModifiedPropertiesWithoutUndo();

			Util.GetOrAddComponent<StoneThrower>(root);

			PlayerBreath breath = Util.GetOrAddComponent<PlayerBreath>(root);
			SerializedObject sb = new SerializedObject(breath);
			sb.FindProperty("_clip").objectReferenceValue = LoadClip("breath_tired");
			sb.ApplyModifiedPropertiesWithoutUndo();

			PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
			Debug.Log("AudioSetup: Player.prefab -> footsteps + breath");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	static void SetClipArray(SerializedObject so, string field, string clipName)
	{
		SerializedProperty prop = so.FindProperty(field);
		if (prop == null)
		{
			Debug.LogError($"AudioSetup: missing field {field}");
			return;
		}

		List<AudioClip> clips = new List<AudioClip>();

		AudioClip first = LoadClip(clipName);
		if (first != null)
			clips.Add(first);

		for (int i = 2; i <= 8; i++)
		{
			AudioClip variant = LoadClip($"{clipName}_{i}");
			if (variant == null)
				break;

			clips.Add(variant);
		}

		prop.arraySize = clips.Count;
		for (int i = 0; i < clips.Count; i++)
			prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
	}


	static void WireDrone(string prefabPath, string clipName, float radiusTiles)
	{
		GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

		try
		{
			Transform child = root.transform.Find("Drone");
			GameObject host;

			if (child != null)
			{
				host = child.gameObject;
			}
			else
			{
				host = new GameObject("Drone");
				host.transform.SetParent(root.transform, false);
			}

			OcclusionSource source = Util.GetOrAddComponent<OcclusionSource>(host);
			SerializedObject so = new SerializedObject(source);
			so.FindProperty("_clearClip").objectReferenceValue = LoadClip(clipName);
			so.FindProperty("_muffledClip").objectReferenceValue = LoadClip(clipName + "_muffled");
			so.FindProperty("_bus").enumValueIndex = (int)Define.Sound.Guide;
			so.FindProperty("_radiusTiles").floatValue = radiusTiles;
			so.ApplyModifiedPropertiesWithoutUndo();

			ProximityDrone drone = Util.GetOrAddComponent<ProximityDrone>(host);
			SerializedObject sd = new SerializedObject(drone);
			sd.FindProperty("_radiusTiles").floatValue = radiusTiles;
			sd.ApplyModifiedPropertiesWithoutUndo();

			PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
			Debug.Log($"AudioSetup: {prefabPath} -> 지속음 {clipName} (반경 {radiusTiles})");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	static void WireStateSounds(string path, string idle, string patrol, string chase)
	{
		GameObject root = PrefabUtility.LoadPrefabContents(path);

		try
		{
			EnemyBase enemy = root.GetComponent<EnemyBase>();
			if (enemy == null)
				return;

			SerializedObject so = new SerializedObject(enemy);
			so.FindProperty("_idleSound").objectReferenceValue = LoadClip(idle);
			so.FindProperty("_patrolSound").objectReferenceValue = LoadClip(patrol);
			so.FindProperty("_chaseSound").objectReferenceValue = LoadClip(chase);
			so.FindProperty("_idleSoundMuffled").objectReferenceValue = LoadClip(idle + "_muffled");
			so.FindProperty("_patrolSoundMuffled").objectReferenceValue = LoadClip(patrol + "_muffled");
			so.FindProperty("_chaseSoundMuffled").objectReferenceValue = LoadClip(chase + "_muffled");
			so.ApplyModifiedPropertiesWithoutUndo();

			PrefabUtility.SaveAsPrefabAsset(root, path);
			Debug.Log($"AudioSetup: {path} -> idle={idle} patrol={patrol} chase={chase}");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	static void WireAmbience()
	{
		UnityEngine.SceneManagement.Scene scene =
			UnityEditor.SceneManagement.EditorSceneManager.OpenScene(InGameScenePath,
				UnityEditor.SceneManagement.OpenSceneMode.Single);

		AmbienceController ambience =
			Object.FindFirstObjectByType<AmbienceController>(FindObjectsInactive.Include);

		if (ambience == null)
		{
			Debug.LogWarning("AudioSetup: 씬에 AmbienceController 없음");
			return;
		}

		SerializedObject so = new SerializedObject(ambience);
		so.FindProperty("_ambientClip").objectReferenceValue = LoadClip("ambient_temple");
		so.ApplyModifiedPropertiesWithoutUndo();

		UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
		UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
		Debug.Log("AudioSetup: 씬 앰비언스 -> ambient_temple");
	}

	static void WireEnemyFootstep(string path, string stepClip, float interval, float jitter,
		float radius, string loopClip, float loopRadius)
	{
		GameObject root = PrefabUtility.LoadPrefabContents(path);

		try
		{
			OcclusionSource source = Util.GetOrAddComponent<OcclusionSource>(root);
			SerializedObject so = new SerializedObject(source);
			so.FindProperty("_clearClip").objectReferenceValue = LoadClip(stepClip);
			so.FindProperty("_muffledClip").objectReferenceValue = LoadClip(stepClip + "_muffled");
			so.FindProperty("_bus").enumValueIndex = (int)Define.Sound.Threat;
			so.FindProperty("_radiusTiles").floatValue = radius;
			so.ApplyModifiedPropertiesWithoutUndo();

			EnemyFootstep steps = Util.GetOrAddComponent<EnemyFootstep>(root);
			SerializedObject sf = new SerializedObject(steps);
			sf.FindProperty("_source").objectReferenceValue = source;
			sf.FindProperty("_interval").floatValue = interval;
			sf.FindProperty("_jitter").floatValue = jitter;
			sf.FindProperty("_audibleRadius").floatValue = radius;
			sf.ApplyModifiedPropertiesWithoutUndo();

			EnemyBase enemy = root.GetComponent<EnemyBase>();
			if (enemy != null && loopClip != null)
			{
				AudioClip loop = LoadClip(loopClip);
				SerializedObject se = new SerializedObject(enemy);
				se.FindProperty("_idleSound").objectReferenceValue = loop;
				se.FindProperty("_patrolSound").objectReferenceValue = loop;
				se.FindProperty("_chaseSound").objectReferenceValue = loop;
				se.FindProperty("_stateSoundRadius").floatValue = loopRadius;
				se.ApplyModifiedPropertiesWithoutUndo();
			}

			PrefabUtility.SaveAsPrefabAsset(root, path);
			Debug.Log($"AudioSetup: {path} -> {stepClip} every {interval}s (jitter {jitter})");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	[MenuItem("LampLight/Audio/Wire Prefabs")]
	public static void WirePrefabs()
	{
		WireOne(ArtifactPrefab, "artifact_ping", 12.0f, true);
		WireOne(ExitDoorPrefab, "exit_ping", 12.0f, false);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("AudioSetup: prefabs wired");
	}

	static void WireOne(string path, string clipName, float radius, bool active)
	{
		AudioClip clear = LoadClip(clipName);
		AudioClip muffled = LoadClip(clipName + "_muffled");

		if (clear == null || muffled == null)
		{
			Debug.LogError($"AudioSetup: missing clips for {clipName}");
			return;
		}

		GameObject root = PrefabUtility.LoadPrefabContents(path);

		try
		{
			OcclusionSource source = Util.GetOrAddComponent<OcclusionSource>(root);
			PingScheduler scheduler = Util.GetOrAddComponent<PingScheduler>(root);

			SerializedObject so = new SerializedObject(source);
			so.FindProperty("_clearClip").objectReferenceValue = clear;
			so.FindProperty("_muffledClip").objectReferenceValue = muffled;
			so.FindProperty("_bus").enumValueIndex = (int)Define.Sound.Guide;
			so.FindProperty("_radiusTiles").floatValue = radius;
			so.ApplyModifiedPropertiesWithoutUndo();

			SerializedObject sp = new SerializedObject(scheduler);
			sp.FindProperty("_source").objectReferenceValue = source;
			sp.FindProperty("_radiusTiles").floatValue = radius;
			sp.FindProperty("_semitoneOffset").floatValue = 0.0f;
			sp.FindProperty("_active").boolValue = active;
			sp.ApplyModifiedPropertiesWithoutUndo();

			ExitDoor door = root.GetComponent<ExitDoor>();
			if (door != null)
			{
				SerializedObject sd = new SerializedObject(door);
				sd.FindProperty("_ping").objectReferenceValue = scheduler;
				sd.ApplyModifiedPropertiesWithoutUndo();
			}

			PrefabUtility.SaveAsPrefabAsset(root, path);
			Debug.Log($"AudioSetup: {path} -> {clipName} (radius {radius}, active {active})");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	static AudioClip LoadClip(string clipName)
	{
		return AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundDir}/{clipName}.wav");
	}

	[MenuItem("LampLight/Audio/Configure WebGL")]
	public static void ConfigureWebGL()
	{
		PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
		PlayerSettings.WebGL.decompressionFallback = true;
		PlayerSettings.WebGL.dataCaching = true;
		PlayerSettings.runInBackground = false;
		PlayerSettings.defaultWebScreenWidth = 1920;
		PlayerSettings.defaultWebScreenHeight = 1080;

		List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
		foreach (string scene in Scenes)
			scenes.Add(new EditorBuildSettingsScene(scene, true));

		EditorBuildSettings.scenes = scenes.ToArray();

		AssetDatabase.SaveAssets();
		Debug.Log($"AudioSetup: WebGL configured (compression {PlayerSettings.WebGL.compressionFormat}, " +
			$"fallback {PlayerSettings.WebGL.decompressionFallback})");
	}

	[MenuItem("LampLight/Audio/Verify")]
	public static void Verify()
	{
		VerifyOne(ArtifactPrefab);
		VerifyOne(ExitDoorPrefab);

		Debug.Log($"AudioSetup: WebGL compression {PlayerSettings.WebGL.compressionFormat}, " +
			$"fallback {PlayerSettings.WebGL.decompressionFallback}, scenes {EditorBuildSettings.scenes.Length}");
	}

	static void VerifyOne(string path)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		if (prefab == null)
		{
			Debug.LogError($"AudioSetup: missing prefab {path}");
			return;
		}

		OcclusionSource source = prefab.GetComponent<OcclusionSource>();
		PingScheduler scheduler = prefab.GetComponent<PingScheduler>();

		if (source == null || scheduler == null)
		{
			Debug.LogError($"AudioSetup: {path} not wired");
			return;
		}

		SerializedObject so = new SerializedObject(source);
		Object clear = so.FindProperty("_clearClip").objectReferenceValue;
		Object muffled = so.FindProperty("_muffledClip").objectReferenceValue;

		SerializedObject sp = new SerializedObject(scheduler);
		bool active = sp.FindProperty("_active").boolValue;
		float radius = sp.FindProperty("_radiusTiles").floatValue;

		Debug.Log($"AudioSetup: {path} clear={ClipName(clear)} muffled={ClipName(muffled)} " +
			$"radius={radius} active={active}");
	}

	static string ClipName(Object clip)
	{
		return clip == null ? "<none>" : clip.name;
	}

	[MenuItem("LampLight/Audio/Build WebGL")]
	public static void BuildWebGL()
	{
		ConfigureWebGL();

		BuildPlayerOptions options = new BuildPlayerOptions();
		options.scenes = Scenes;
		options.locationPathName = BuildDir;
		options.target = BuildTarget.WebGL;
		options.targetGroup = BuildTargetGroup.WebGL;
		options.options = BuildOptions.None;

		BuildReport report = BuildPipeline.BuildPlayer(options);
		BuildSummary summary = report.summary;

		Debug.Log($"AudioSetup: build {summary.result} size={summary.totalSize / (1024 * 1024)}MB " +
			$"errors={summary.totalErrors} time={summary.totalTime}");

		if (summary.result != BuildResult.Succeeded)
			EditorApplication.Exit(1);
	}
}
