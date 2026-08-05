using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
	const string DefaultOutput = "Build/WebGL";

	[MenuItem("LampLight/Build WebGL for itch.io")]
	public static void BuildFromMenu()
	{
		Run(DefaultOutput);
	}

	public static void Build()
	{
		string output = Arg("-buildOut", DefaultOutput);
		BuildReport report = Run(output);

		if (report == null || report.summary.result != BuildResult.Succeeded)
		{
			EditorApplication.Exit(1);
			return;
		}

		EditorApplication.Exit(0);
	}

	static BuildReport Run(string output)
	{
		string[] scenes = EditorBuildSettings.scenes
			.Where(scene => scene.enabled)
			.Select(scene => scene.path)
			.ToArray();

		if (scenes.Length == 0)
		{
			Debug.LogError("WebGLBuild: 빌드 씬 목록이 비었다");
			return null;
		}

		PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
		PlayerSettings.WebGL.decompressionFallback = true;
		PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
		PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
		PlayerSettings.runInBackground = true;
		PlayerSettings.defaultWebScreenWidth = 1920;
		PlayerSettings.defaultWebScreenHeight = 1080;

		Directory.CreateDirectory(output);

		BuildPlayerOptions options = new BuildPlayerOptions
		{
			scenes = scenes,
			locationPathName = output,
			target = BuildTarget.WebGL,
			targetGroup = BuildTargetGroup.WebGL,
			options = BuildOptions.None
		};

		Debug.Log($"WebGLBuild: {scenes.Length} scenes -> {output}");
		BuildReport report = BuildPipeline.BuildPlayer(options);
		BuildSummary summary = report.summary;

		Debug.Log($"WebGLBuild: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, "
			+ $"{summary.totalTime}, errors {summary.totalErrors}");

		return report;
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
