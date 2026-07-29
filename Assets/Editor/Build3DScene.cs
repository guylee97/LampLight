using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class Build3DScene
{
	const string ScenePath = "Assets/Scenes/InGame3D.unity";
	const string MapDataPath = "Assets/Resources/Data/MapData.json";
	const string SettingsDir = "Assets/Settings";
	const string GeneratedDir = "Assets/Settings/Generated3D";

	const string SideSprite = "Assets/Art/Tiles/wall_side.png";
	const string TopSprite = "Assets/Art/Tiles/wall_top_02.png";
	const string BulkSprite = "Assets/Art/Tiles/wall_01.png";

	static readonly (string Keyword, string AssetPath)[] SpriteByTilesetKeyword =
	{
		("noisy", "Assets/Art/Tiles/floor_02_noisy.png"),
		("옆면", "Assets/Art/Tiles/wall_side.png"),
		("윗면", "Assets/Art/Tiles/wall_top_02.png"),
		("벽", "Assets/Art/Tiles/wall_01.png"),
		("wall", "Assets/Art/Tiles/wall_01.png"),
		("바닥", "Assets/Art/Tiles/floor_03.png"),
		("floor", "Assets/Art/Tiles/floor_03.png"),
	};

	const float WallHeight = 2.2f;

	const string DefaultFloorSprite = "Assets/Art/Tiles/floor_03.png";

	const float CameraPitch = 66.0f;
	static readonly Vector3 CameraOffset = new Vector3(0, 17, -7.5f);
	const float CameraFov = 55.0f;

	static int FocusCol { get { return ArgInt("-focusCol", 28); } }
	static int FocusRow { get { return ArgInt("-focusRow", 13); } }

	[MenuItem("LampLight/Build 3D Scene")]
	public static void Build()
	{
		MapData map = LoadMap();
		if (map == null)
			return;

		UsePipeline(EnsureUrp3D());

		Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		Vector3 focus = TileToWorld(map, FocusCol, FocusRow);

		BuildGround(map);
		BuildWalls(map);
		BuildLighting();
		BuildPlayer(focus);
		BuildCamera(focus);

		RenderSettings.ambientMode = AmbientMode.Flat;
		float amb = ArgInt("-ambientPct", 16) / 100.0f;
		RenderSettings.ambientLight = new Color(amb, amb, amb * 1.06f);
		RenderSettings.fog = false;

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene, ScenePath);
		Debug.Log($"Build3DScene: saved {ScenePath} ({map.width}x{map.height}, focus {focus})");
	}


	[MenuItem("LampLight/Renderer/Use 2D (InGame)")]
	public static void Use2D()
	{
		RenderPipelineAsset urp2d =
			AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>($"{SettingsDir}/UniversalRP.asset");

		if (urp2d == null)
		{
			Debug.LogError("Build3DScene: UniversalRP.asset 없음");
			return;
		}

		UsePipeline(urp2d);
		Debug.Log("Build3DScene: 2D 파이프라인으로 전환");
	}

	[MenuItem("LampLight/Renderer/Use 3D (InGame3D)")]
	public static void Use3D()
	{
		UsePipeline(EnsureUrp3D());
		Debug.Log("Build3DScene: 3D 파이프라인으로 전환");
	}

	static void UsePipeline(RenderPipelineAsset asset)
	{
		GraphicsSettings.defaultRenderPipeline = asset;

		int current = QualitySettings.GetQualityLevel();
		for (int i = 0; i < QualitySettings.names.Length; i++)
		{
			QualitySettings.SetQualityLevel(i, false);
			QualitySettings.renderPipeline = asset;
		}

		QualitySettings.SetQualityLevel(current, false);
		AssetDatabase.SaveAssets();
	}

	static UniversalRenderPipelineAsset EnsureUrp3D()
	{
		string rendererPath = $"{SettingsDir}/URP3D_Renderer.asset";
		string urpPath = $"{SettingsDir}/URP3D.asset";

		UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
		if (data == null)
		{
			data = ScriptableObject.CreateInstance<UniversalRendererData>();
			AssetDatabase.CreateAsset(data, rendererPath);
		}

		UniversalRenderPipelineAsset urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
		if (urp == null)
		{
			urp = UniversalRenderPipelineAsset.Create(data);
			AssetDatabase.CreateAsset(urp, urpPath);
		}

		urp.supportsHDR = true;
		urp.shadowDistance = 60.0f;

		SerializedObject so = new SerializedObject(urp);
		SetIfPresent(so, "m_AdditionalLightsRenderingMode", 1);
		SetIfPresent(so, "m_AdditionalLightsPerObjectLimit", 8);
		SetIfPresent(so, "m_AdditionalLightShadowsSupported", 1);
		SetIfPresent(so, "m_MainLightShadowsSupported", 1);
		so.ApplyModifiedPropertiesWithoutUndo();

		EditorUtility.SetDirty(urp);
		AssetDatabase.SaveAssets();
		return urp;
	}


	static Vector3 TileToWorld(MapData map, int col, int row)
	{
		return new Vector3(col + 0.5f, 0, (map.height - 1 - row) + 0.5f);
	}

	static int WallGid(MapData map, int col, int row)
	{
		if (col < 0 || col >= map.width || row < 0 || row >= map.height)
			return -1;

		return map.walls[row * map.width + col];
	}

	static bool IsFootprint(MapData map, int col, int row)
	{
		if (col < 0 || col >= map.width || row < 0 || row >= map.height)
			return true;

		int gid = WallGid(map, col, row);
		if (gid == 0)
			return false;

		string texture = TexturePathForGid(map, gid);
		return texture != null && texture != SideSprite;
	}


	static string TexturePathForGid(MapData map, int gid)
	{
		string tilesetName = map.GetTilesetName(gid);
		if (string.IsNullOrEmpty(tilesetName))
			return null;

		string lowered = tilesetName.ToLowerInvariant();
		foreach ((string keyword, string assetPath) in SpriteByTilesetKeyword)
		{
			if (lowered.Contains(keyword))
				return assetPath;
		}

		return null;
	}

	static string TopTextureFor(MapData map, int gid)
	{
		string texture = TexturePathForGid(map, gid);
		return texture == BulkSprite ? TopSprite : texture;
	}

	class SubMeshBuilder
	{
		public readonly List<Vector3> Verts = new List<Vector3>();
		public readonly List<Vector2> Uvs = new List<Vector2>();
		readonly Dictionary<string, List<int>> _trisByTexture = new Dictionary<string, List<int>>();
		readonly List<string> _order = new List<string>();

		public List<int> For(string texturePath)
		{
			List<int> list;
			if (_trisByTexture.TryGetValue(texturePath, out list))
				return list;

			list = new List<int>();
			_trisByTexture[texturePath] = list;
			_order.Add(texturePath);
			return list;
		}

		public Mesh ToMesh(string name, out Material[] materials)
		{
			Mesh mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
			mesh.SetVertices(Verts);
			mesh.SetUVs(0, Uvs);
			mesh.subMeshCount = _order.Count;

			materials = new Material[_order.Count];
			for (int i = 0; i < _order.Count; i++)
			{
				mesh.SetTriangles(_trisByTexture[_order[i]], i);
				materials[i] = MakeMaterial(_order[i]);
			}

			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			return mesh;
		}
	}


	static void BuildGround(MapData map)
	{
		SubMeshBuilder builder = new SubMeshBuilder();

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (IsFootprint(map, col, row))
					continue;

				int gid = map.floor[row * map.width + col];
				string texture = gid != 0 ? TexturePathForGid(map, gid) : null;

				if (texture == null)
					texture = DefaultFloorSprite;

				float x = col;
				float z = map.height - 1 - row;
				AddHorizontalQuad(builder, builder.For(texture), x, z, 0);
			}
		}

		GameObject go = new GameObject("Ground");
		Material[] materials;
		Mesh mesh = builder.ToMesh("GroundMesh", out materials);

		go.AddComponent<MeshFilter>().sharedMesh = mesh;
		go.AddComponent<MeshRenderer>().sharedMaterials = materials;

		AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/GroundMesh.asset");
		Debug.Log($"Build3DScene: ground {mesh.subMeshCount} submeshes, {builder.Verts.Count} verts");
	}

	static void BuildWalls(MapData map)
	{
		SubMeshBuilder builder = new SubMeshBuilder();

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (!IsFootprint(map, col, row))
					continue;

				string topTexture = TopTextureFor(map, WallGid(map, col, row));
				if (topTexture == null)
					continue;

				float x = col;
				float z = map.height - 1 - row;

				AddHorizontalQuad(builder, builder.For(topTexture), x, z, WallHeight);

				List<int> sideTris = builder.For(SideSprite);
				if (!IsFootprint(map, col, row + 1)) AddSideFace(builder, sideTris, x, z, Vector3.back);
				if (!IsFootprint(map, col, row - 1)) AddSideFace(builder, sideTris, x, z, Vector3.forward);
				if (!IsFootprint(map, col - 1, row)) AddSideFace(builder, sideTris, x, z, Vector3.left);
				if (!IsFootprint(map, col + 1, row)) AddSideFace(builder, sideTris, x, z, Vector3.right);
			}
		}

		GameObject go = new GameObject("Walls");
		Material[] materials;
		Mesh mesh = builder.ToMesh("WallMesh", out materials);

		go.AddComponent<MeshFilter>().sharedMesh = mesh;
		go.AddComponent<MeshRenderer>().sharedMaterials = materials;

		AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/WallMesh.asset");
		Debug.Log($"Build3DScene: walls {mesh.subMeshCount} submeshes, {builder.Verts.Count} verts");
	}

	static void AddHorizontalQuad(SubMeshBuilder builder, List<int> tris, float x, float z, float y)
	{
		int b = builder.Verts.Count;
		builder.Verts.Add(new Vector3(x, y, z));
		builder.Verts.Add(new Vector3(x + 1, y, z));
		builder.Verts.Add(new Vector3(x + 1, y, z + 1));
		builder.Verts.Add(new Vector3(x, y, z + 1));

		builder.Uvs.Add(new Vector2(0, 0));
		builder.Uvs.Add(new Vector2(1, 0));
		builder.Uvs.Add(new Vector2(1, 1));
		builder.Uvs.Add(new Vector2(0, 1));

		tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
	}

	static void AddSideFace(SubMeshBuilder builder, List<int> tris, float x, float z, Vector3 dir)
	{
		int b = builder.Verts.Count;

		Vector3 a0, a1;
		if (dir == Vector3.back)         { a0 = new Vector3(x + 1, 0, z);     a1 = new Vector3(x, 0, z); }
		else if (dir == Vector3.forward) { a0 = new Vector3(x, 0, z + 1);     a1 = new Vector3(x + 1, 0, z + 1); }
		else if (dir == Vector3.left)    { a0 = new Vector3(x, 0, z);         a1 = new Vector3(x, 0, z + 1); }
		else                             { a0 = new Vector3(x + 1, 0, z + 1); a1 = new Vector3(x + 1, 0, z); }

		builder.Verts.Add(a0);
		builder.Verts.Add(a1);
		builder.Verts.Add(a1 + Vector3.up * WallHeight);
		builder.Verts.Add(a0 + Vector3.up * WallHeight);

		builder.Uvs.Add(new Vector2(0, 0));
		builder.Uvs.Add(new Vector2(1, 0));
		builder.Uvs.Add(new Vector2(1, 1));
		builder.Uvs.Add(new Vector2(0, 1));

		tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
	}


	static Material MakeMaterial(string spritePath)
	{
		string name = "M3D_" + System.IO.Path.GetFileNameWithoutExtension(spritePath);
		string path = $"{GeneratedDir}/{name}.mat";

		Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
		if (tex == null)
			Debug.LogError($"Build3DScene: texture not found at {spritePath}");

		Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
		Material mat = existing != null ? existing : new Material(Shader.Find("Universal Render Pipeline/Lit"));

		mat.SetTexture("_BaseMap", tex);
		mat.mainTexture = tex;
		mat.SetFloat("_Smoothness", 0.0f);
		mat.SetFloat("_Metallic", 0.0f);

		if (existing == null)
			AssetDatabase.CreateAsset(mat, path);
		else
			EditorUtility.SetDirty(mat);

		return AssetDatabase.LoadAssetAtPath<Material>(path);
	}


	static void BuildLighting()
	{
		GameObject go = new GameObject("Fill");
		Light light = go.AddComponent<Light>();
		light.type = LightType.Directional;
		light.color = Color.white;
		light.intensity = ArgInt("-fillPct", 35) / 100.0f;
		light.shadows = LightShadows.None;
		go.transform.rotation = Quaternion.Euler(30, 25, 0);
	}

	static void BuildPlayer(Vector3 focus)
	{
		GameObject root = new GameObject("Player");
		root.transform.position = focus;

		GameObject visual = new GameObject("Visual");
		visual.transform.SetParent(root.transform, false);
		visual.transform.localPosition = new Vector3(0, 0.75f, 0);
		visual.transform.rotation = Quaternion.Euler(CameraPitch, 0, 0);

		SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
		sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Player/player_idle_n.png");
		sr.shadowCastingMode = ShadowCastingMode.TwoSided;

		GameObject lantern = new GameObject("Lantern");
		lantern.transform.SetParent(root.transform, false);
		lantern.transform.localPosition = new Vector3(0, 1.0f, 0.2f);
		lantern.transform.rotation = Quaternion.Euler(32, 0, 0);

		Light spot = lantern.AddComponent<Light>();
		spot.type = LightType.Spot;
		spot.color = new Color(1.0f, 0.97f, 0.92f);
		spot.intensity = 11.0f;
		spot.range = 24.0f;
		spot.spotAngle = 72.0f;
		spot.innerSpotAngle = 28.0f;
		spot.shadows = LightShadows.Soft;
		spot.shadowStrength = 0.95f;
	}

	static void BuildCamera(Vector3 focus)
	{
		GameObject go = new GameObject("Main Camera");
		go.tag = "MainCamera";

		Camera camera = go.AddComponent<Camera>();
		camera.orthographic = false;
		camera.fieldOfView = CameraFov;
		camera.nearClipPlane = 0.3f;
		camera.farClipPlane = 200.0f;
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = Color.black;

		go.AddComponent<UniversalAdditionalCameraData>();

		go.transform.position = focus + CameraOffset;
		go.transform.rotation = Quaternion.Euler(CameraPitch, 0, 0);
	}


	static void SetIfPresent(SerializedObject so, string property, int value)
	{
		SerializedProperty found = so.FindProperty(property);
		if (found != null)
			found.intValue = value;
		else
			Debug.LogWarning($"Build3DScene: URP 애셋에 {property} 없음 (버전 차이)");
	}

	static MapData LoadMap()
	{
		System.IO.Directory.CreateDirectory(GeneratedDir);

		TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>(MapDataPath);
		if (text == null)
		{
			Debug.LogError($"Build3DScene: {MapDataPath} not found");
			return null;
		}

		return JsonUtility.FromJson<MapData>(text.text);
	}

	static int ArgInt(string name, int fallback)
	{
		string[] args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length - 1; i++)
		{
			int parsed;
			if (args[i] == name && int.TryParse(args[i + 1], out parsed))
				return parsed;
		}

		return fallback;
	}

}
