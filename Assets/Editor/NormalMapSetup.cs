using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

public static class NormalMapSetup
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string TileDir = "Assets/Art/Tiles";

	static readonly string[] Tiles =
	{
		"wall_side", "wall_top_02", "wall_01", "floor_03", "floor_02_noisy",
	};

	[MenuItem("LampLight/Set Up Tile Normal Maps")]
	public static void Run()
	{
		int attached = 0;
		foreach (string tile in Tiles)
		{
			if (Attach(tile))
				attached++;
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		int lit = EnableOnLights();
		Debug.Log($"NormalMapSetup: 스프라이트 {attached}개에 노멀맵 부착, 조명 {lit}개에 활성화");
	}

	static bool Attach(string tile)
	{
		string basePath = $"{TileDir}/{tile}.png";
		string normalPath = $"{TileDir}/{tile}_n.png";

		TextureImporter baseImporter = AssetImporter.GetAtPath(basePath) as TextureImporter;
		Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

		if (baseImporter == null || normal == null)
		{
			Debug.LogError($"NormalMapSetup: {basePath} 또는 {normalPath} 없음");
			return false;
		}

		TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
		if (normalImporter != null &&
			(normalImporter.textureType != TextureImporterType.NormalMap || normalImporter.sRGBTexture))
		{
			normalImporter.textureType = TextureImporterType.NormalMap;
			normalImporter.sRGBTexture = false;
			normalImporter.SaveAndReimport();
		}

		baseImporter.secondarySpriteTextures = new[]
		{
			new SecondarySpriteTexture { name = "_NormalMap", texture = normal },
		};
		baseImporter.SaveAndReimport();
		return true;
	}

	static int EnableOnLights()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		int count = 0;
		foreach (Light2D light in Object.FindObjectsByType<Light2D>(
			FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (light.lightType == Light2D.LightType.Global)
				continue;

			SerializedObject so = new SerializedObject(light);
			SerializedProperty quality = so.FindProperty("m_NormalMapQuality");
			SerializedProperty use = so.FindProperty("m_UseNormalMap");

			if (quality == null)
			{
				Debug.LogWarning($"NormalMapSetup: {light.name}에 m_NormalMapQuality 없음 (버전 차이)");
				continue;
			}

			quality.intValue = (int)Light2D.NormalMapQuality.Accurate;
			if (use != null)
				use.boolValue = true;

			so.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(light);
			count++;
		}

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		return count;
	}
}
