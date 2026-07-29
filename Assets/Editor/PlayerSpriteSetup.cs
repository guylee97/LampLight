using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PlayerSpriteSetup
{
	const string ScenePath = "Assets/Scenes/InGame.unity";
	const string PlayerArtDir = "Assets/Art/Player";
	const string VisualName = "Visual";
	const string GlowName = "PlayerGlow";
	const float LanternRange = 10.0f;
	const float LanternAngle = 70.0f;

	const float LanternIntensity = 1.55f;

	const int SortingOrder = 20;

	static readonly Vector2 ColliderSize = new Vector2(0.55f, 0.35f);
	static readonly Vector2 ColliderOffset = new Vector2(0, 0.18f);

	static readonly string[] SpriteNames =
	{
		"player_idle_e",
		"player_idle_ne",
		"player_idle_n",
		"player_idle_nw",
		"player_idle_w",
		"player_idle_sw",
		"player_idle_s",
		"player_idle_se",
	};

	[MenuItem("LampLight/Set Up Player Sprites")]
	public static void Run()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		if (player == null)
		{
			Debug.LogError("PlayerSpriteSetup: no PlayerController in scene");
			return;
		}

		Sprite[] sprites = LoadSprites();
		if (sprites == null)
			return;

		GameObject visual = EnsureVisualChild(player.gameObject);

		ApplyRenderer(visual, sprites);
		ApplyDirectionalSprite(visual, sprites);
		ApplyCollider(player.gameObject);
		RemoveWalkBob(player.gameObject);
		ApplyLanternLook(player.gameObject);
		ApplyController(player, visual);

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		Debug.Log("PlayerSpriteSetup: done");
	}

	static Sprite[] LoadSprites()
	{
		Sprite[] sprites = new Sprite[SpriteNames.Length];

		for (int i = 0; i < SpriteNames.Length; i++)
		{
			string path = $"{PlayerArtDir}/{SpriteNames[i]}.png";
			sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);

			if (sprites[i] == null)
			{
				Debug.LogError($"PlayerSpriteSetup: sprite not found at {path}");
				return null;
			}
		}

		return sprites;
	}

	static GameObject EnsureVisualChild(GameObject player)
	{
		Transform found = player.transform.Find(VisualName);
		if (found != null)
			return found.gameObject;

		GameObject visual = new GameObject(VisualName);
		visual.transform.SetParent(player.transform, false);
		visual.transform.localPosition = Vector3.zero;

		SpriteRenderer old = player.GetComponent<SpriteRenderer>();
		if (old != null)
			Object.DestroyImmediate(old);

		DirectionalSprite oldDirectional = player.GetComponent<DirectionalSprite>();
		if (oldDirectional != null)
			Object.DestroyImmediate(oldDirectional);

		return visual;
	}

	static void RemoveWalkBob(GameObject player)
	{
		WalkBob bob = player.GetComponent<WalkBob>();
		if (bob == null)
			return;

		Object.DestroyImmediate(bob);
		Debug.Log("PlayerSpriteSetup: WalkBob 제거 (등불 흔들림으로 대체)");
	}

	static void ApplyLanternLook(GameObject player)
	{
		Lamp lamp = player.GetComponentInChildren<Lamp>();
		if (lamp == null)
		{
			Debug.LogWarning("PlayerSpriteSetup: Lamp 없음 — 조명 설정 건너뜀");
			return;
		}

		SerializedObject lso = new SerializedObject(lamp);
		lso.FindProperty("_range").floatValue = LanternRange;
		lso.FindProperty("_angle").floatValue = LanternAngle;
		lso.FindProperty("_intensity").floatValue = LanternIntensity;
		lso.FindProperty("_innerRangeRatio").floatValue = 0.25f;
		lso.FindProperty("_innerAngleRatio").floatValue = 0.75f;
		lso.ApplyModifiedPropertiesWithoutUndo();

		Light2D lanternLight = lamp.GetComponent<Light2D>();
		if (lanternLight != null)
		{
			ConfigureLight(lanternLight, LanternRange, LanternRange * 0.25f, LanternIntensity,
				new Color(1.0f, 0.93f, 0.75f), 0.85f, true);
			lanternLight.pointLightOuterAngle = LanternAngle;
			lanternLight.pointLightInnerAngle = LanternAngle * 0.75f;
			EditorUtility.SetDirty(lanternLight);
		}

		LanternSway sway = lamp.GetComponent<LanternSway>();
		if (sway != null)
			Object.DestroyImmediate(sway);

		Transform found = player.transform.Find(GlowName);
		GameObject glow = found != null ? found.gameObject : new GameObject(GlowName);
		glow.transform.SetParent(player.transform, false);
		glow.transform.localPosition = new Vector3(0, 0.45f, 0);

		Light2D glowLight = Util.GetOrAddComponent<Light2D>(glow);
		ConfigureLight(glowLight, 3.6f, 0.4f, 1.15f,
			new Color(1.0f, 0.88f, 0.65f), 1.0f, true);

		Debug.Log($"PlayerSpriteSetup: 등불 콘 {LanternAngle}도 r{LanternRange} + 글로우 r3.6");
	}

	static void ConfigureLight(Light2D light, float outer, float inner, float intensity,
		Color color, float falloff, bool shadows)
	{
		light.lightType = Light2D.LightType.Point;
		light.pointLightOuterRadius = outer;
		light.pointLightInnerRadius = inner;
		light.pointLightOuterAngle = 360.0f;
		light.pointLightInnerAngle = 360.0f;
		light.intensity = intensity;
		light.color = color;
		light.falloffIntensity = falloff;
		light.shadowsEnabled = shadows;
		light.shadowIntensity = 1.0f;
		EditorUtility.SetDirty(light);
	}

	static void ApplyRenderer(GameObject go, Sprite[] sprites)
	{
		SpriteRenderer renderer = Util.GetOrAddComponent<SpriteRenderer>(go);
		renderer.sprite = sprites[(int)Define.Direction8.S];
		renderer.sortingOrder = SortingOrder;
		EditorUtility.SetDirty(renderer);
	}

	static void ApplyDirectionalSprite(GameObject go, Sprite[] sprites)
	{
		DirectionalSprite directional = Util.GetOrAddComponent<DirectionalSprite>(go);

		SerializedObject so = new SerializedObject(directional);
		so.FindProperty("_renderer").objectReferenceValue = go.GetComponent<SpriteRenderer>();
		so.FindProperty("_direction").enumValueIndex = (int)Define.Direction8.S;

		SerializedProperty array = so.FindProperty("_sprites");
		array.arraySize = sprites.Length;
		for (int i = 0; i < sprites.Length; i++)
			array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];

		so.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(directional);
	}

	static void ApplyCollider(GameObject go)
	{
		CapsuleCollider2D collider = go.GetComponent<CapsuleCollider2D>();
		if (collider == null)
		{
			Debug.LogWarning("PlayerSpriteSetup: no CapsuleCollider2D on player, adding one");
			collider = go.AddComponent<CapsuleCollider2D>();
		}

		collider.direction = CapsuleDirection2D.Horizontal;
		collider.size = ColliderSize;
		collider.offset = ColliderOffset;
		EditorUtility.SetDirty(collider);
		Debug.Log($"PlayerSpriteSetup: collider size {collider.size} offset {collider.offset}");
	}

	static void ApplyController(PlayerController player, GameObject visual)
	{
		player.transform.rotation = Quaternion.identity;

		SerializedObject so = new SerializedObject(player);
		so.FindProperty("_directionalSprite").objectReferenceValue = visual.GetComponent<DirectionalSprite>();
		so.FindProperty("_startDirection").enumValueIndex = (int)Define.Direction8.S;
		so.ApplyModifiedPropertiesWithoutUndo();

		EditorUtility.SetDirty(player);
	}
}
