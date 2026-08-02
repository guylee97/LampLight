using UnityEditor;
using UnityEngine;

public static class CharacterPrefabSetup
{
	const int ColliderPixelsPerUnit = 64;

	static readonly (string Prefab, string Character)[] Mapping =
	{
		("Assets/Resources/Prefabs/Player.prefab", "player"),
		("Assets/Resources/Prefabs/DefaultZombie.prefab", "zombie_walker"),
		("Assets/Resources/Prefabs/ActiveZombie.prefab", "zombie_wanderer"),
		("Assets/Resources/Prefabs/RunnerZombie.prefab", "zombie_runner"),
	};

	[MenuItem("LampLight/Bind Character Sprites To Prefabs")]
	public static void Run()
	{
		CharacterCatalog.Invalidate();

		int bound = 0;

		foreach ((string prefabPath, string characterKey) in Mapping)
		{
			if (Bind(prefabPath, characterKey))
				bound++;
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log($"CharacterPrefabSetup: 프리팹 {bound}개 연결");
	}

	static bool Bind(string prefabPath, string characterKey)
	{
		CharacterSpec spec = CharacterCatalog.Get(characterKey);
		if (spec == null)
		{
			Debug.LogError($"CharacterPrefabSetup: 카탈로그에 '{characterKey}' 없음");
			return false;
		}

		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (prefab == null)
		{
			Debug.LogError($"CharacterPrefabSetup: {prefabPath} 없음");
			return false;
		}

		GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

		SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
		if (renderer == null)
			renderer = root.GetComponentInChildren<SpriteRenderer>();

		if (renderer == null)
		{
			Debug.LogError($"CharacterPrefabSetup: {prefabPath} 에 SpriteRenderer 없음");
			PrefabUtility.UnloadPrefabContents(root);
			return false;
		}

		GameObject host = renderer.gameObject;

		DirectionalSpriteAnimator animator = host.GetComponent<DirectionalSpriteAnimator>();
		if (animator == null)
			animator = host.AddComponent<DirectionalSpriteAnimator>();

		SerializedObject so = new SerializedObject(animator);
		so.FindProperty("_characterKey").stringValue = characterKey;
		so.FindProperty("_renderer").objectReferenceValue = renderer;
		so.ApplyModifiedPropertiesWithoutUndo();

		Animator legacy = root.GetComponentInChildren<Animator>();
		if (legacy != null)
			legacy.enabled = false;

		ApplyCollider(root, spec);
		ApplyFirstSprite(renderer, spec);

		PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
		PrefabUtility.UnloadPrefabContents(root);

		Debug.Log($"CharacterPrefabSetup: {characterKey} -> {prefabPath} "
			+ $"(충돌 {spec.colliderW}x{spec.colliderH}px)");
		return true;
	}

	static void ApplyCollider(GameObject root, CharacterSpec spec)
	{
		if (spec.colliderW <= 0.0f || spec.colliderH <= 0.0f)
			return;

		Vector2 size = spec.ColliderSize(ColliderPixelsPerUnit);

		CapsuleCollider2D capsule = root.GetComponent<CapsuleCollider2D>();
		if (capsule != null)
		{
			capsule.size = size;
			capsule.offset = Vector2.zero;
			EditorUtility.SetDirty(capsule);
			return;
		}

		BoxCollider2D box = root.GetComponent<BoxCollider2D>();
		if (box != null)
		{
			box.size = size;
			box.offset = Vector2.zero;
			EditorUtility.SetDirty(box);
			return;
		}

		CircleCollider2D circle = root.GetComponent<CircleCollider2D>();
		if (circle != null)
		{
			circle.radius = Mathf.Min(size.x, size.y) * 0.5f;
			EditorUtility.SetDirty(circle);
		}
	}

	static void ApplyFirstSprite(SpriteRenderer renderer, CharacterSpec spec)
	{
		CharacterState idle = spec.State(DirectionalSpriteAnimator.StateIdle);
		if (idle == null)
			return;

		foreach (Sprite sprite in Resources.LoadAll<Sprite>(idle.resource))
		{
			if (sprite.name == CharacterCatalog.SpriteName(spec.key, idle.name, "s", 0))
			{
				renderer.sprite = sprite;
				EditorUtility.SetDirty(renderer);
				return;
			}
		}
	}
}
