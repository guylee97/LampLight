using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HeadlessShot
{
	const string KeyPending = "HeadlessShot.Pending";
	const string KeyOut = "HeadlessShot.Out";
	const string KeyWidth = "HeadlessShot.Width";
	const string KeyHeight = "HeadlessShot.Height";
	const string KeyWait = "HeadlessShot.Wait";

	const string KeySeconds = "HeadlessShot.Seconds";
	const string KeySeed = "HeadlessShot.Seed";
	const string KeyDebug = "HeadlessShot.Debug";
	const string KeyPose = "HeadlessShot.Pose";

	const int StartFrame = 30;

	static int _frame;
	static float _playStartTime;

	public static void Capture()
	{
		string scene = Arg("-shotScene", null);
		if (string.IsNullOrEmpty(scene))
		{
			Debug.LogError("HeadlessShot: -shotScene 이 없다");
			EditorApplication.Exit(2);
			return;
		}

		string outPath = Arg("-shotOut", "shot.png");
		int width = ParseInt(Arg("-shotW", "1920"), 1920);
		int height = ParseInt(Arg("-shotH", "1080"), 1080);
		int wait = ParseInt(Arg("-shotWait", "180"), 180);

		SessionState.SetBool(KeyPending, true);
		SessionState.SetString(KeyOut, outPath);
		SessionState.SetInt(KeyWidth, width);
		SessionState.SetInt(KeyHeight, height);
		SessionState.SetInt(KeyWait, wait);
		SessionState.SetFloat(KeySeconds, ParseFloat(Arg("-shotSeconds", "0"), 0.0f));
		int seed = ParseInt(Arg("-shotSeed", "-1"), -1);
		SessionState.SetInt(KeySeed, seed);
		InGameScene.SeedOverride = seed;
		SessionState.SetBool(KeyDebug, Arg("-shotColliders", null) != null);
		SessionState.SetFloat(KeyPose, ParseFloat(Arg("-shotPose", "0"), 0.0f));

		Debug.Log($"HeadlessShot: open {scene} -> {outPath} ({width}x{height}, wait {wait})");
		EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);

		Hook();
		EditorApplication.EnterPlaymode();
	}

	[InitializeOnLoadMethod]
	static void OnDomainLoad()
	{
		if (SessionState.GetBool(KeyPending, false) == false)
			return;

		InGameScene.SeedOverride = SessionState.GetInt(KeySeed, -1);
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

		if (!EditorApplication.isPlaying)
		{
			_frame++;
			if (_frame > 900)
			{
				Debug.LogError("HeadlessShot: 플레이 모드 진입 실패");
				Finish(3);
			}

			return;
		}

		_frame++;

		if (_frame == StartFrame)
		{
			ForceStartPlaying();
			_playStartTime = Time.realtimeSinceStartup;
		}

		if (_frame < SessionState.GetInt(KeyWait, 180))
			return;

		float seconds = SessionState.GetFloat(KeySeconds, 0.0f);
		if (seconds > 0.0f && Time.realtimeSinceStartup - _playStartTime < seconds)
			return;

		int code = 0;
		try
		{
			Shoot();
		}
		catch (Exception e)
		{
			Debug.LogError($"HeadlessShot: {e}");
			code = 4;
		}

		Finish(code);
	}

	static void Finish(int code)
	{
		SessionState.SetBool(KeyPending, false);
		EditorApplication.update -= OnUpdate;
		EditorApplication.Exit(code);
	}

	static void ForceStartPlaying()
	{
		const System.Reflection.BindingFlags Flags =
			System.Reflection.BindingFlags.Instance |
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.NonPublic;

		int started = 0;
		foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
			FindObjectsInactive.Exclude, FindObjectsSortMode.None))
		{
			if (behaviour == null)
				continue;

			System.Reflection.MethodInfo method =
				behaviour.GetType().GetMethod("StartPlaying", Flags, null, Type.EmptyTypes, null);

			if (method == null)
				continue;

			method.Invoke(behaviour, null);
			started++;
			Debug.Log($"HeadlessShot: StartPlaying() on {behaviour.GetType().Name}");
		}

		if (started == 0)
			Debug.Log("HeadlessShot: StartPlaying() 대상 없음 (이미 플레이 상태이거나 해당 없음)");
	}

	static void Shoot()
	{
		Camera camera = Camera.main;
		if (camera == null)
			camera = UnityEngine.Object.FindFirstObjectByType<Camera>();

		if (camera == null)
			throw new Exception("씬에 카메라가 없다");

		int width = SessionState.GetInt(KeyWidth, 1920);
		int height = SessionState.GetInt(KeyHeight, 1080);
		string outPath = SessionState.GetString(KeyOut, "shot.png");

		RouteOverlayCanvases(camera);

		float pose = SessionState.GetFloat(KeyPose, 0.0f);
		if (pose > 0.0f)
			PoseEnemiesBesidePlayer(pose);

		if (SessionState.GetBool(KeyDebug, false))
		{
			GameObject viewer = new GameObject("DebugColliderView");
			viewer.AddComponent<DebugColliderView>().Build();
			Debug.Log("HeadlessShot: collider overlay on");
		}

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

		string dir = Path.GetDirectoryName(outPath);
		if (!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);

		File.WriteAllBytes(outPath, tex.EncodeToPNG());
		Debug.Log($"HeadlessShot: wrote {outPath} ({new FileInfo(outPath).Length} bytes) via camera '{camera.name}'");
	}

	static void PoseEnemiesBesidePlayer(float distance)
	{
		PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
		if (player == null)
			return;

		Vector3 origin = player.transform.position;
		string[] order = { "Walker", "Wanderer", "Runner" };
		Vector3[] slots =
		{
			new Vector3(-distance, 0.0f, 0.0f),
			new Vector3(distance, 0.0f, 0.0f),
			new Vector3(0.0f, distance, 0.0f),
		};

		EnemyBase[] enemies = UnityEngine.Object.FindObjectsByType<EnemyBase>(
			FindObjectsSortMode.InstanceID);

		for (int i = 0; i < order.Length; i++)
		{
			foreach (EnemyBase enemy in enemies)
			{
				if (enemy == null || enemy.name.Contains(order[i]) == false)
					continue;

				enemy.transform.position = origin + slots[i];
				Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
				if (body != null)
					body.simulated = false;

				Debug.Log($"HeadlessShot: posed {enemy.name} at {slots[i]}");
				break;
			}
		}

		Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
		if (playerBody != null)
			playerBody.simulated = false;
	}

	static void RouteOverlayCanvases(Camera camera)
	{
		foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(
			FindObjectsInactive.Exclude, FindObjectsSortMode.None))
		{
			if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
				continue;

			canvas.renderMode = RenderMode.ScreenSpaceCamera;
			canvas.worldCamera = camera;
			canvas.planeDistance = 1.0f;
			Canvas.ForceUpdateCanvases();
		}
	}

	static int ParseInt(string value, int fallback)
	{
		int parsed;
		return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
			? parsed
			: fallback;
	}

	static float ParseFloat(string value, float fallback)
	{
		float parsed;
		return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
			? parsed
			: fallback;
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
}
