using UnityEngine;

public static class YokaiFactory
{
	public const string LitMaterialResource = "Image/M_SpriteLit";

	/// Player.prefab 의 발치 캡슐과 같은 값이다.
	public static readonly Vector2 ActorFootSize = new Vector2(0.34f, 0.18f);

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

		// 플레이어 발치 캡슐과 똑같이 준다. 크면 플레이어가 지나는 틈에 끼고,
		// 작으면 판정이 막힘이라 부르는 칸에 몸이 들어가 길찾기가 성립하지 않는다.
		// 같은 모양이면 요괴는 정확히 플레이어가 가는 곳까지 간다.
		CapsuleCollider2D collider = go.AddComponent<CapsuleCollider2D>();
		collider.direction = CapsuleDirection2D.Horizontal;
		collider.size = ActorFootSize;
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
