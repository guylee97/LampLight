using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TempleSceneSetup
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string DecoRootName = "@Deco";
	const string LegacyDressingName = "@Dressing";

	[MenuItem("LampLight/Wire Temple Deco Into Scene")]
	public static void Run()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		int removed = RemoveLegacyDressing();
		MapDecoPlacer placer = EnsurePlacer();
		bool linked = LinkToSceneScript(placer);

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);

		Debug.Log($"TempleSceneSetup: 옛 소품 {removed}개 제거, MapDecoPlacer 준비 완료, "
			+ $"InGameScene 연결 {(linked ? "성공" : "실패")}");
	}

	static int RemoveLegacyDressing()
	{
		GameObject legacy = GameObject.Find(LegacyDressingName);
		if (legacy == null)
			return 0;

		int children = legacy.transform.childCount;
		Object.DestroyImmediate(legacy);
		return children;
	}

	static MapDecoPlacer EnsurePlacer()
	{
		MapDecoPlacer existing = Object.FindFirstObjectByType<MapDecoPlacer>(FindObjectsInactive.Include);
		if (existing != null)
			return existing;

		GameObject root = GameObject.Find(DecoRootName);
		if (root == null)
			root = new GameObject(DecoRootName);

		return root.AddComponent<MapDecoPlacer>();
	}

	static bool LinkToSceneScript(MapDecoPlacer placer)
	{
		InGameScene scene = Object.FindFirstObjectByType<InGameScene>(FindObjectsInactive.Include);
		if (scene == null)
			return false;

		SerializedObject so = new SerializedObject(scene);
		SerializedProperty property = so.FindProperty("_deco");
		if (property == null)
			return false;

		property.objectReferenceValue = placer;
		so.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(scene);
		return true;
	}
}
