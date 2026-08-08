using UnityEngine;

public static class YokaiFactory
{
	public const string LitMaterialResource = "Image/M_SpriteLit";

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

		// 통행 가능 여부는 플레이어 발치 상자(0.62 x 0.32)로 굽는다. 요괴 몸통이
		// 그보다 크면 길찾기는 갈 수 있다고 하고 물리는 막아서 좁은 데서 낀다.
		// 어느 방향으로든 그 상자에 들어가는 가장 큰 원이 곧 이 반지름이다.
		CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
		collider.radius = MapCoord.ActorHalfHeight;
		collider.offset = new Vector2(0.0f, MapCoord.ActorFootOffset);

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
}
