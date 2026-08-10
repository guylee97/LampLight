using System.Collections.Generic;
using UnityEngine;

/// 이미 본 대사는 다시 뜨지 않는다. 재시작이 잦은 게임에서 같은 문단을 두 번
/// 읽히면 그 다음부터는 아무도 읽지 않는다.
public static class DialogueMemory
{
	static readonly HashSet<string> Seen = new HashSet<string>();

	static bool s_loaded;
	static bool s_persists = true;

	static string Key
	{
		get
		{
			string key = DialogueTable.Book.seenKey;
			return string.IsNullOrEmpty(key) ? "onelantern.dialogue.seen.v3" : key;
		}
	}

	public static bool HasSeen(string id)
	{
		EnsureLoaded();
		return string.IsNullOrEmpty(id) == false && Seen.Contains(id);
	}

	public static void MarkSeen(string id)
	{
		if (string.IsNullOrEmpty(id))
			return;

		EnsureLoaded();

		if (Seen.Add(id) == false)
			return;

		Save();
	}

	/// 새 판을 처음부터 보고 싶을 때. 디버그와 테스트가 쓴다.
	public static void Forget()
	{
		Seen.Clear();
		s_loaded = true;

		if (s_persists)
			PlayerPrefs.DeleteKey(Key);
	}

	static void EnsureLoaded()
	{
		if (s_loaded)
			return;

		s_loaded = true;

		string stored = s_persists ? PlayerPrefs.GetString(Key, string.Empty) : string.Empty;
		if (string.IsNullOrEmpty(stored))
			return;

		foreach (string id in stored.Split('\n'))
		{
			if (string.IsNullOrEmpty(id) == false)
				Seen.Add(id);
		}
	}

	static void Save()
	{
		if (s_persists == false)
			return;

		// 저장이 막힌 환경(웹 저장소 거부 등)에서는 메모리로만 굴린다.
		try
		{
			PlayerPrefs.SetString(Key, string.Join("\n", Seen));
			PlayerPrefs.Save();
		}
		catch (System.Exception error)
		{
			s_persists = false;
			Debug.LogWarning($"DialogueMemory: 저장 실패, 메모리로만 기억한다 — {error.Message}");
		}
	}
}
