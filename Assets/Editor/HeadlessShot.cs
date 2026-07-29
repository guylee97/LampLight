using System;
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

	const int StartFrame = 30;

	static int _frame;

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

		Debug.Log($"HeadlessShot: open {scene} -> {outPath} ({width}x{height}, wait {wait})");
		EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);

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
			ForceStartPlaying();

		if (_frame < SessionState.GetInt(KeyWait, 180))
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

	static int ParseInt(string value, int fallback)
	{
		int parsed;
		return int.TryParse(value, out parsed) ? parsed : fallback;
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
