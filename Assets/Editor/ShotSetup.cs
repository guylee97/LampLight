using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ShotSetup
{
	const string ScenePath = "Assets/Scenes/InGame.unity";

	static int FocusCol { get { return ArgInt("-focusCol", 28); } }
	static int FocusRow { get { return ArgInt("-focusRow", 13); } }

	[MenuItem("LampLight/Prepare AS-IS Shot")]
	public static void Prepare()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		MapData map = LoadMap();
		if (map == null)
			return;

		Vector3 focus = new Vector3(FocusCol + 0.5f, map.height - 1 - FocusRow + 0.5f, 0);
		Debug.Log($"ShotSetup: focus tile ({FocusCol},{FocusRow}) -> world {focus}");

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		if (player != null)
		{
			player.transform.position = focus;
			player.transform.rotation = Quaternion.identity;
			EditorUtility.SetDirty(player);
		}

		Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
		if (camera != null)
		{
			camera.transform.position = new Vector3(focus.x, focus.y, -10);
			EditorUtility.SetDirty(camera);
		}

		MatchAmbient();
		BakeLampSettings();

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		Debug.Log("ShotSetup: done");
	}

	static float ComparableGlobalIntensity { get { return ArgInt("-globalLightPct", 55) / 100.0f; } }

	static void MatchAmbient()
	{
		foreach (UnityEngine.Rendering.Universal.Light2D light in
			Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(
				FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (light.lightType != UnityEngine.Rendering.Universal.Light2D.LightType.Global)
				continue;

			light.intensity = ComparableGlobalIntensity;
			EditorUtility.SetDirty(light);
			Debug.Log($"ShotSetup: global light -> {ComparableGlobalIntensity}");
		}
	}

	static void BakeLampSettings()
	{
		Lamp lamp = Object.FindFirstObjectByType<Lamp>();
		if (lamp == null)
		{
			Debug.LogWarning("ShotSetup: 씬에 Lamp 없음");
			return;
		}

		Light2D light = lamp.GetComponent<Light2D>();
		if (light == null)
		{
			Debug.LogWarning("ShotSetup: Lamp에 Light2D 없음");
			return;
		}

		SerializedObject so = new SerializedObject(lamp);
		float range = so.FindProperty("_range").floatValue;
		float angle = so.FindProperty("_angle").floatValue;
		float intensity = so.FindProperty("_intensity").floatValue;
		float shadowIntensity = so.FindProperty("_shadowIntensity").floatValue;
		float innerRangeRatio = so.FindProperty("_innerRangeRatio").floatValue;
		float innerAngleRatio = so.FindProperty("_innerAngleRatio").floatValue;

		light.lightType = Light2D.LightType.Point;
		light.intensity = intensity;
		light.pointLightOuterRadius = range;
		light.pointLightInnerRadius = range * innerRangeRatio;
		light.pointLightOuterAngle = angle;
		light.pointLightInnerAngle = angle * innerAngleRatio;
		light.shadowsEnabled = true;
		light.shadowIntensity = shadowIntensity;

		lamp.transform.rotation = Quaternion.identity;

		EditorUtility.SetDirty(light);
		Debug.Log($"ShotSetup: lamp baked — range {range}, angle {angle}, intensity {intensity}");
	}

	static MapData LoadMap()
	{
		TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Data/MapData.json");
		if (text == null)
		{
			Debug.LogError("ShotSetup: MapData.json not found");
			return null;
		}

		return JsonUtility.FromJson<MapData>(text.text);
	}

	static int ArgInt(string name, int fallback)
	{
		string[] args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length - 1; i++)
		{
			int parsed;
			if (args[i] == name && int.TryParse(args[i + 1], out parsed))
				return parsed;
		}

		return fallback;
	}

}
