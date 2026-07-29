using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WalkCapture
{
	const string Scene2D = "Assets/Scenes/InGame.unity";
	const string Scene3D = "Assets/Scenes/InGame3D.unity";

	const string KeyPending = "WalkCapture.Pending";
	const string KeyMode = "WalkCapture.Mode";
	const string KeyOut = "WalkCapture.Out";
	const string KeyFrom = "WalkCapture.From";
	const string KeyTo = "WalkCapture.To";
	const string KeySteps = "WalkCapture.Steps";
	const string KeyW = "WalkCapture.W";
	const string KeyH = "WalkCapture.H";

	const int SettleFrames = 90;

	const float CameraPitch = 66.0f;
	static readonly Vector3 CameraOffset3D = new Vector3(0, 17, -7.5f);

	static int _tick;
	static int _captured;

	public static void Run()
	{
		string mode = Arg("-walkMode", "3d");

		SessionState.SetBool(KeyPending, true);
		SessionState.SetString(KeyMode, mode);
		SessionState.SetString(KeyOut, Arg("-walkOut", "frames"));
		SessionState.SetString(KeyFrom, Arg("-walkFrom", "28,10"));
		SessionState.SetString(KeyTo, Arg("-walkTo", "28,20"));
		SessionState.SetInt(KeySteps, ArgInt("-walkSteps", 28));
		SessionState.SetInt(KeyW, ArgInt("-walkW", 1120));
		SessionState.SetInt(KeyH, ArgInt("-walkH", 630));

		Directory.CreateDirectory(SessionState.GetString(KeyOut, "frames"));
		EditorSceneManager.OpenScene(mode == "3d" ? Scene3D : Scene2D, OpenSceneMode.Single);

		Hook();
		EditorApplication.EnterPlaymode();
	}

	[InitializeOnLoadMethod]
	static void OnDomainLoad()
	{
		if (SessionState.GetBool(KeyPending, false))
			Hook();
	}

	static void Hook()
	{
		EditorApplication.update -= OnUpdate;
		EditorApplication.update += OnUpdate;
	}

	static void OnUpdate()
	{
		if (!SessionState.GetBool(KeyPending, false))
		{
			EditorApplication.update -= OnUpdate;
			return;
		}

		_tick++;

		if (!EditorApplication.isPlaying)
		{
			if (_tick > 900)
			{
				Debug.LogError("WalkCapture: 플레이 모드 진입 실패");
				Finish(3);
			}

			return;
		}

		if (_tick < SettleFrames)
			return;

		int steps = SessionState.GetInt(KeySteps, 28);
		if (_captured >= steps)
		{
			Debug.Log($"WalkCapture: {_captured} frames -> {SessionState.GetString(KeyOut, "?")}");
			Finish(0);
			return;
		}

		try
		{
			CaptureStep(_captured, steps);
		}
		catch (Exception e)
		{
			Debug.LogError($"WalkCapture: {e}");
			Finish(4);
			return;
		}

		_captured++;
	}

	static void CaptureStep(int index, int steps)
	{
		bool is3D = SessionState.GetString(KeyMode, "3d") == "3d";
		Vector2Int from = ParseTile(SessionState.GetString(KeyFrom, "28,10"));
		Vector2Int to = ParseTile(SessionState.GetString(KeyTo, "28,20"));

		MapData map = LoadMap();
		if (map == null)
			throw new Exception("MapData.json을 읽지 못했다");

		Transform player = FindPlayer(is3D);
		Camera camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
		if (player == null || camera == null)
			throw new Exception($"player={player} camera={camera}");

		CameraController follow = camera.GetComponent<CameraController>();
		if (follow != null && follow.enabled)
			follow.enabled = false;

		float t = steps <= 1 ? 0 : (float)index / (steps - 1);
		float col = Mathf.Lerp(from.x, to.x, t);
		float row = Mathf.Lerp(from.y, to.y, t);

		if (is3D)
		{
			Vector3 focus = new Vector3(col + 0.5f, 0, (map.height - 1 - row) + 0.5f);
			player.position = focus;
			camera.transform.position = focus + CameraOffset3D;
			camera.transform.rotation = Quaternion.Euler(CameraPitch, 0, 0);
		}
		else
		{
			Vector3 flat = new Vector3(col + 0.5f, (map.height - 1 - row) + 0.5f, 0);
			player.position = flat;
			camera.transform.position = new Vector3(flat.x, flat.y, -10);
		}

		Shoot(camera, Path.Combine(SessionState.GetString(KeyOut, "frames"), $"f{index:D3}.png"));
	}

	static MapData LoadMap()
	{
		TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Data/MapData.json");
		return text == null ? null : JsonUtility.FromJson<MapData>(text.text);
	}

	static Transform FindPlayer(bool is3D)
	{
		if (is3D)
		{
			foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(
				FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (go.name == "Player" && go.transform.parent == null)
					return go.transform;
			}

			return null;
		}

		PlayerController controller = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
		return controller != null ? controller.transform : null;
	}

	static void Shoot(Camera camera, string path)
	{
		int width = SessionState.GetInt(KeyW, 1120);
		int height = SessionState.GetInt(KeyH, 630);

		RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
		rt.Create();

		RenderTexture previousTarget = camera.targetTexture;
		RenderTexture previousActive = RenderTexture.active;

		camera.targetTexture = rt;
		camera.Render();

		RenderTexture.active = rt;
		Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
		tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		tex.Apply();

		camera.targetTexture = previousTarget;
		RenderTexture.active = previousActive;

		File.WriteAllBytes(path, tex.EncodeToPNG());

		UnityEngine.Object.DestroyImmediate(tex);
		rt.Release();
		UnityEngine.Object.DestroyImmediate(rt);
	}

	static void Finish(int code)
	{
		SessionState.SetBool(KeyPending, false);
		EditorApplication.update -= OnUpdate;
		EditorApplication.Exit(code);
	}

	static Vector2Int ParseTile(string raw)
	{
		string[] parts = (raw ?? "").Split(',');
		int col, row;
		if (parts.Length == 2 && int.TryParse(parts[0], out col) && int.TryParse(parts[1], out row))
			return new Vector2Int(col, row);

		return new Vector2Int(28, 13);
	}

	static string Arg(string name, string fallback)
	{
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] == name)
				return args[i + 1];
		}

		return fallback;
	}

	static int ArgInt(string name, int fallback)
	{
		int parsed;
		return int.TryParse(Arg(name, null), out parsed) ? parsed : fallback;
	}
}
