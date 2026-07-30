using System.IO;
using UnityEditor;
using UnityEngine;

public static class ActorPhysicsSetup
{
	const string MaterialDir = "Assets/Resources/Physics";
	const string MaterialPath = MaterialDir + "/Frictionless.physicsMaterial2D";

	static readonly (string Path, Define.Layer Layer)[] ActorPrefabs =
	{
		("Assets/Resources/Prefabs/Player.prefab", Define.Layer.Player),
		("Assets/Resources/Prefabs/DefaultZombie.prefab", Define.Layer.Enemy),
		("Assets/Resources/Prefabs/ActiveZombie.prefab", Define.Layer.Enemy),
	};

	static readonly Vector2 BodySize = new Vector2(0.58f, 0.4f);
	static readonly Vector2 BodyOffset = new Vector2(0, -0.28f);

	[MenuItem("LampLight/Setup Actor Physics")]
	public static void Setup()
	{
		PhysicsMaterial2D material = LoadOrCreateMaterial();

		Physics2D.IgnoreLayerCollision((int)Define.Layer.Enemy, (int)Define.Layer.Enemy, true);

		foreach ((string path, Define.Layer layer) in ActorPrefabs)
			Apply(path, layer, material);

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

	static void Apply(string path, Define.Layer layer, PhysicsMaterial2D material)
	{
		GameObject prefab = PrefabUtility.LoadPrefabContents(path);
		if (prefab == null)
		{
			Debug.LogError($"ActorPhysicsSetup: {path} not found");
			return;
		}

		prefab.layer = (int)layer;

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
		Debug.Log($"ActorPhysicsSetup: {path} (layer {(int)layer})");
	}
}
