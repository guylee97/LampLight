using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DebugOverlaySetup
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string RootName = "@Debug";

	[MenuItem("LampLight/Add Debug Overlay To Scene")]
	public static void Run()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		DebugOverlay existing = Object.FindFirstObjectByType<DebugOverlay>(FindObjectsInactive.Include);
		if (existing != null)
		{
			Debug.Log($"DebugOverlaySetup: 이미 '{existing.gameObject.name}' 에 붙어 있다");
			return;
		}

		GameObject root = GameObject.Find(RootName);
		if (root == null)
			root = new GameObject(RootName);

		root.AddComponent<DebugOverlay>();

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);

		Debug.Log($"DebugOverlaySetup: {RootName} 에 DebugOverlay 추가");
	}
}
