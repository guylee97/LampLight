using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

public static class PixelPerfectSetup
{
	public const int AssetsPPU = 64;
	public const int RefWidth = 640;
	public const int RefHeight = 360;

	const string ScenePath = "Assets/Scenes/InGame.unity";

	[MenuItem("LampLight/Apply Pixel Perfect Camera")]
	public static void ApplyFromMenu()
	{
		Camera camera = Resolve();
		if (camera == null)
		{
			Debug.LogError("PixelPerfectSetup: 씬에 카메라가 없다");
			return;
		}

		Configure(camera);
		EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
	}

	public static void Apply()
	{
		int width = ParseInt(Arg("-ppcX", RefWidth.ToString()), RefWidth);
		int height = ParseInt(Arg("-ppcY", RefHeight.ToString()), RefHeight);

		EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		Camera camera = Resolve();
		if (camera == null)
		{
			Debug.LogError("PixelPerfectSetup: 씬에 카메라가 없다");
			EditorApplication.Exit(2);
			return;
		}

		Configure(camera, width, height);
		EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);

		if (EditorSceneManager.SaveOpenScenes() == false)
		{
			Debug.LogError($"PixelPerfectSetup: {ScenePath} 저장에 실패했다");
			EditorApplication.Exit(3);
			return;
		}

		EditorApplication.Exit(0);
	}

	static Camera Resolve()
	{
		Camera camera = Camera.main;
		return camera != null ? camera : UnityEngine.Object.FindFirstObjectByType<Camera>();
	}

	static void Configure(Camera camera)
	{
		Configure(camera, RefWidth, RefHeight);
	}

	static void Configure(Camera camera, int width, int height)
	{
		PixelPerfectCamera pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
		if (pixelPerfect == null)
			pixelPerfect = camera.gameObject.AddComponent<PixelPerfectCamera>();

		pixelPerfect.assetsPPU = AssetsPPU;
		pixelPerfect.refResolutionX = width;
		pixelPerfect.refResolutionY = height;
		pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
		pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;

		EditorUtility.SetDirty(pixelPerfect);
		Debug.Log($"PixelPerfectSetup: {camera.name} PPU {AssetsPPU}, ref {width}x{height}");
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
