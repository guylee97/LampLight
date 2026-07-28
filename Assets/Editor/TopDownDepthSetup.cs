using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TopDownDepthSetup
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";

	const int FloorSortingOrder = -10;
	const int GroundLevelSortingOrder = 10;

	[MenuItem("LampLight/Enable Top-Down Depth")]
	public static void Run()
	{
		ApplyCustomAxisSorting();

		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		ApplyTilemapSettings();
		ApplyWallShadows();
		ReportSortingOrders();

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		AssetDatabase.SaveAssets();
		Debug.Log("TopDownDepthSetup: done");
	}

	static void ApplyCustomAxisSorting()
	{
		Object[] assets = AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsPath);
		if (assets == null || assets.Length == 0)
		{
			Debug.LogError("TopDownDepthSetup: GraphicsSettings.asset not found");
			return;
		}

		SerializedObject so = new SerializedObject(assets[0]);
		so.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
		so.FindProperty("m_TransparencySortAxis").vector3Value = new Vector3(0, 1, 0);
		so.ApplyModifiedPropertiesWithoutUndo();
		Debug.Log("TopDownDepthSetup: transparency sort = CustomAxis (0,1,0)");
	}

	static void ApplyTilemapSettings()
	{
		foreach (TilemapRenderer renderer in Object.FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			renderer.mode = TilemapRenderer.Mode.Individual;
			renderer.sortingOrder = renderer.name == "Floor" ? FloorSortingOrder : GroundLevelSortingOrder;
			EditorUtility.SetDirty(renderer);
			Debug.Log($"TopDownDepthSetup: {renderer.name} mode=Individual order={renderer.sortingOrder}");
		}
	}

	static void ApplyWallShadows()
	{
		foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (tilemap.name != "Wall")
				continue;

			Collider2D collider = tilemap.GetComponent<Collider2D>();
			if (collider == null)
			{
				Debug.LogError($"TopDownDepthSetup: {tilemap.name} has no Collider2D to derive shadows from");
				return;
			}

			ShadowCaster2D caster = tilemap.GetComponent<ShadowCaster2D>();
			if (caster == null)
				caster = tilemap.gameObject.AddComponent<ShadowCaster2D>();

			SerializedObject so = new SerializedObject(caster);
			so.FindProperty("m_ShadowCastingSource").intValue = 2;
			so.FindProperty("m_ShadowShape2DComponent").objectReferenceValue = collider;
			so.FindProperty("m_CastsShadows").boolValue = true;
			so.FindProperty("m_SelfShadows").boolValue = false;
			so.ApplyModifiedPropertiesWithoutUndo();

			EditorUtility.SetDirty(caster);
			Debug.Log($"TopDownDepthSetup: ShadowCaster2D on {tilemap.name} using {collider.GetType().Name}");
			return;
		}

		Debug.LogError("TopDownDepthSetup: no tilemap named Wall");
	}

	static void ReportSortingOrders()
	{
		foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			Debug.Log($"TopDownDepthSetup: sprite {renderer.name} layer='{renderer.sortingLayerName}' order={renderer.sortingOrder}");
	}
}
