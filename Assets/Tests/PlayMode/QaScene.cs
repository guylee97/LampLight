using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class QaScene
{
	public const string InGame = "InGame";
	public const int DefaultSeed = 20260801;
	public const int WallLayer = 10;

	public static string ReportDir
	{
		get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "QAReports")); }
	}

	public static void AllowHeadlessInput()
	{
		Application.runInBackground = true;
		InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
		InputSystem.settings.editorInputBehaviorInPlayMode =
			InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
	}

	public static IEnumerator Load()
	{
		return Load(DefaultSeed);
	}

	public static IEnumerator Load(int seed)
	{
		AllowHeadlessInput();
		InGameScene.SeedOverride = seed;
		SceneManager.LoadScene(InGame, LoadSceneMode.Single);

		for (int i = 0; i < 5; i++)
			yield return null;

		// 도입 대사는 게임을 멈춘다. 테스트는 대사가 아니라 플레이를 보는 것이라
		// 사람이 넘기듯 치워 놓고 시작한다.
		UI_Dialogue.Clear();
		yield return null;

		yield return new WaitForFixedUpdate();
		Physics2D.SyncTransforms();
	}

	public static string WriteReport(string name, string body)
	{
		Directory.CreateDirectory(ReportDir);
		string path = Path.Combine(ReportDir, name);
		File.WriteAllText(path, body);
		return path;
	}

	public static string WritePng(string name, Texture2D texture)
	{
		Directory.CreateDirectory(ReportDir);
		string path = Path.Combine(ReportDir, name);
		File.WriteAllBytes(path, texture.EncodeToPNG());
		return path;
	}
}
