using System.IO;
using UnityEditor;
using UnityEngine;

public static class ActorPhysicsSetup
{
	const string MaterialDir = "Assets/Resources/Physics";
	const string MaterialPath = MaterialDir + "/Frictionless.physicsMaterial2D";

	static readonly string[] ActorPrefabs =
	{
		"Assets/Resources/Prefabs/Player.prefab",
		"Assets/Resources/Prefabs/DefaultZombie.prefab",
		"Assets/Resources/Prefabs/ActiveZombie.prefab",
	};

	static readonly Vector2 BodySize = new Vector2(0.58f, 0.4f);
	static readonly Vector2 BodyOffset = new Vector2(0, -0.28f);

	[MenuItem("LampLight/Setup Actor Physics")]
	public static void Setup()
	{
		PhysicsMaterial2D material = LoadOrCreateMaterial();

		foreach (string path in ActorPrefabs)
			Apply(path, material);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("ActorPhysicsSetup: done");
	}

	static PhysicsMaterial2D LoadOrCreateMaterial()
	{
		Directory.CreateDirectory(MaterialDir);

		PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);
		if (material == null)
		{
			material = new PhysicsMaterial2D("Frictionless");
			AssetDatabase.CreateAsset(material, MaterialPath);
		}

		material.friction = 0;
		material.bounciness = 0;
		EditorUtility.SetDirty(material);
		return material;
	}

	static void Apply(string path, PhysicsMaterial2D material)
	{
		GameObject prefab = PrefabUtility.LoadPrefabContents(path);
		if (prefab == null)
		{
			Debug.LogError($"ActorPhysicsSetup: {path} not found");
			return;
		}

		CapsuleCollider2D capsule = prefab.GetComponent<CapsuleCollider2D>();
		if (capsule == null)
		{
			Collider2D stale = prefab.GetComponent<Collider2D>();
			if (stale != null)
				Object.DestroyImmediate(stale);

			capsule = prefab.AddComponent<CapsuleCollider2D>();
		}

		capsule.direction = CapsuleDirection2D.Horizontal;
		capsule.size = BodySize;
		capsule.offset = BodyOffset;
		capsule.isTrigger = false;

		Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
		if (body == null)
			body = prefab.AddComponent<Rigidbody2D>();

		body.bodyType = RigidbodyType2D.Dynamic;
		body.gravityScale = 0;
		body.interpolation = RigidbodyInterpolation2D.Interpolate;
		body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		body.constraints = RigidbodyConstraints2D.FreezeRotation;
		body.sharedMaterial = material;

		PrefabUtility.SaveAsPrefabAsset(prefab, path);
		PrefabUtility.UnloadPrefabContents(prefab);
		Debug.Log($"ActorPhysicsSetup: {path}");
	}
}
