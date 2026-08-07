using UnityEngine;

public static class YokaiFactory
{
	public const int PixelsPerUnit = 32;
	public const string LitMaterialResource = "Image/M_SpriteLit";

	const float FallbackColliderTiles = 1.25f;

	public static GameObject Build(YokaiSpec spec, Transform parent)
	{
		string characterKey = ResolveCharacter(spec.CharacterKey);
		GameObject go = new GameObject($"Yokai_{characterKey}");
		go.transform.SetParent(parent, false);
		go.layer = (int)Define.Layer.Enemy;

		GameObject art = new GameObject("Art");
		art.transform.SetParent(go.transform, false);

		SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
		renderer.sortingOrder = 0;

		Material lit = Resources.Load<Material>(LitMaterialResource);
		if (lit != null)
			renderer.sharedMaterial = lit;

		Rigidbody2D body = go.AddComponent<Rigidbody2D>();
		body.bodyType = RigidbodyType2D.Dynamic;
		body.gravityScale = 0.0f;
		body.interpolation = RigidbodyInterpolation2D.Interpolate;
		body.constraints = RigidbodyConstraints2D.FreezeRotation;
		body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

		CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
		collider.radius = ColliderRadius(characterKey);
		collider.offset = Vector2.zero;

		DirectionalSpriteAnimator animator = go.AddComponent<DirectionalSpriteAnimator>();
		animator.UseRenderer(renderer);
		animator.SetCharacter(characterKey);
		animator.SetStutter(0.45f, 1.0f);

		renderer.color = spec.Tint;

		MaskYokai yokai = go.AddComponent<MaskYokai>();
		yokai.UseSpec(spec);
		return go;
	}

	static string ResolveCharacter(string characterKey)
	{
		if (CharacterCatalog.Get(characterKey) != null)
			return characterKey;

		string fallback = YokaiTable.At(0).CharacterKey;
		Debug.LogWarning(
			$"YokaiFactory: '{characterKey}' 스프라이트가 아직 없어 '{fallback}' 로 대체한다");

		return fallback;
	}

	static float ColliderRadius(string characterKey)
	{
		CharacterSpec spec = CharacterCatalog.Get(characterKey);

		if (spec == null || spec.colliderW <= 0.0f)
			return FallbackColliderTiles * 0.5f;

		return spec.colliderW / PixelsPerUnit * 0.5f;
	}
}
