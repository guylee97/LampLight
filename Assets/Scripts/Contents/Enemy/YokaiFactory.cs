using UnityEngine;

public static class YokaiFactory
{
	public const string AgwiKey = "agwi";
	public const int PixelsPerUnit = 32;

	const float FallbackColliderTiles = 1.25f;

	public static GameObject Build(string characterKey, Transform parent)
	{
		GameObject go = new GameObject($"Yokai_{characterKey}");
		go.transform.SetParent(parent, false);
		go.layer = (int)Define.Layer.Enemy;

		GameObject art = new GameObject("Art");
		art.transform.SetParent(go.transform, false);

		SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
		renderer.sortingOrder = 0;

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

		go.AddComponent<MaskYokai>();
		return go;
	}

	static float ColliderRadius(string characterKey)
	{
		CharacterSpec spec = CharacterCatalog.Get(characterKey);

		if (spec == null || spec.colliderW <= 0.0f)
			return FallbackColliderTiles * 0.5f;

		return spec.colliderW / PixelsPerUnit * 0.5f;
	}
}
