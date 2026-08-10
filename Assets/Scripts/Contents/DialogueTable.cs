using System;
using UnityEngine;

[Serializable]
public class DialogueTrigger
{
	public string voice;
	public string id;
	public int level;
	public string anchor;
	public float radius;
	public bool once;
	public int requireCollected;
	public bool requireAll;
	public string[] lines;

	public bool Allows(int collected, int required)
	{
		if (requireAll)
			return required > 0 && collected >= required;

		return collected >= requireCollected;
	}
}

[Serializable]
public class DialogueOpening
{
	public string voice;
	public string id;
	public int level;
	public string[] lines;
}

[Serializable]
public class DialoguePickup
{
	public string more;
	public string done;
}

[Serializable]
public class DialogueBeat
{
	public string voice;
	public string id;
	public string[] lines;
}

[Serializable]
public class DialogueBook
{
	public string seenKey;
	public DialogueOpening[] opening;
	public DialoguePickup pickup;
	public DialogueBeat firstRun;
	public DialogueBeat ending;
	public DialogueBeat lampOut;
	public DialogueTrigger[] triggers;
}

/// 대사는 전부 Resources/Data/dialogue_triggers.json 에 있다. 코드에 문자열을 박지 않는다.
public static class DialogueTable
{
	public const string Resource = "Data/dialogue_triggers";

	static DialogueBook s_book;

	public static DialogueBook Book
	{
		get
		{
			if (s_book == null)
				Load();

			return s_book;
		}
	}

	public static void Invalidate()
	{
		s_book = null;
	}

	static void Load()
	{
		TextAsset text = Resources.Load<TextAsset>(Resource);

		if (text == null)
		{
			Debug.LogError($"DialogueTable: Resources/{Resource}.json 없음");
			s_book = new DialogueBook();
			return;
		}

		s_book = JsonUtility.FromJson<DialogueBook>(text.text);

		if (s_book == null)
		{
			Debug.LogError($"DialogueTable: {Resource} 파싱 실패");
			s_book = new DialogueBook();
		}
	}

	/// level 0 은 "1전각이 아닌 모든 전각".
	public static DialogueOpening OpeningEntry(int level)
	{
		DialogueOpening[] all = Book.opening;
		if (all == null)
			return null;

		foreach (DialogueOpening entry in all)
		{
			if (entry.level == level)
				return entry;
		}

		foreach (DialogueOpening entry in all)
		{
			if (entry.level == 0)
				return entry;
		}

		return null;
	}

	public static UI_Dialogue.Voice Speaker(string voice)
	{
		return UI_Dialogue.VoiceOf(voice);
	}

	public static string PickupLine(int collected, int required)
	{
		DialoguePickup pickup = Book.pickup;
		if (pickup == null)
			return null;

		return collected >= required ? pickup.done : pickup.more;
	}
}
