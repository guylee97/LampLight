using System.IO;
using UnityEditor;
using UnityEngine;

public static class MapPrefabBuilder
{
	const string PrefabDir = "Assets/Resources/Prefabs/Map";
	const string ObjectArtDir = "Assets/Art/Objects";

	[MenuItem("LampLight/Build Map Prefabs")]
	public static void Build()
	{
		Directory.CreateDirectory(PrefabDir);

		BuildArtifact();
		BuildExitDoor();

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("MapPrefabBuilder: done");
	}

	static Sprite LoadSprite(string name)
	{
		string path = $"{ObjectArtDir}/{name}.png";
		Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
		if (sprite == null)
			Debug.LogError($"MapPrefabBuilder: sprite not found at {path}");
		return sprite;
	}

	static void BuildArtifact()
	{
		GameObject go = new GameObject("Artifact");

		SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
		renderer.sprite = LoadSprite("artifact_01");
		renderer.sortingOrder = 10;

		CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
		collider.isTrigger = true;
		collider.radius = 0.6f;

		Artifact artifact = go.AddComponent<Artifact>();
		SerializedObject so = new SerializedObject(artifact);
		so.FindProperty("_renderer").objectReferenceValue = renderer;
		so.FindProperty("_collectNoiseRadius").floatValue = 12.0f;
		so.ApplyModifiedPropertiesWithoutUndo();

		Save(go, "Artifact");
	}

	static void BuildExitDoor()
	{
		GameObject go = new GameObject("ExitDoor");

		Sprite locked = LoadSprite("exit_door_locked");
		Sprite open = LoadSprite("exit_door_open");

		SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
		renderer.sprite = locked;
		renderer.sortingOrder = 5;

		BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
		collider.isTrigger = true;
		collider.size = new Vector2(1.2f, 1.2f);

		ExitDoor door = go.AddComponent<ExitDoor>();
		SerializedObject so = new SerializedObject(door);
		so.FindProperty("_lockedSprite").objectReferenceValue = locked;
		so.FindProperty("_openSprite").objectReferenceValue = open;
		so.ApplyModifiedPropertiesWithoutUndo();

		Save(go, "ExitDoor");
	}

	static void Save(GameObject go, string name)
	{
		string path = $"{PrefabDir}/{name}.prefab";
		PrefabUtility.SaveAsPrefabAsset(go, path);
		Object.DestroyImmediate(go);
		Debug.Log($"MapPrefabBuilder: saved {path}");
	}
}
