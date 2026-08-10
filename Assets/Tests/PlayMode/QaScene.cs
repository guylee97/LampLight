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

	/// 대사 자체를 보는 시험만 쓴다. 사물이 말을 걸면 게임이 멈추므로
	/// 나머지 시험에서는 켜두면 안 된다.
	public static IEnumerator LoadWithDialogue()
	{
		DialogueMemory.Forget();
		return Load(DefaultSeed, true);
	}

	public static IEnumerator Load(int seed)
	{
		return Load(seed, false);
	}

	public static IEnumerator Load(int seed, bool withDialogue)
	{
		AllowHeadlessInput();

		// 사물 대사는 다가가는 즉시 발동하고 timeScale 을 0 으로 잡는다.
		// 봇이 지나가다 걸리면 WaitForSeconds 를 쓰는 시험이 영영 안 깨어난다.
		// 기본은 전부 읽은 것으로 표시해 재워둔다.
		if (withDialogue == false)
			SilenceDialogue();

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

	static void SilenceDialogue()
	{
		DialogueTrigger[] triggers = DialogueTable.Book.triggers;
		if (triggers == null)
			return;

		foreach (DialogueTrigger trigger in triggers)
			DialogueMemory.MarkSeen(trigger.id);

		DialogueBeat firstRun = DialogueTable.Book.firstRun;
		if (firstRun != null)
			DialogueMemory.MarkSeen(firstRun.id);
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
