using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DialogueTriggerTests
{
	[SetUp]
	public void ForgetWhatWasRead()
	{
		DialogueMemory.Forget();
	}

	[UnityTest]
	public IEnumerator EveryTriggerFindsItsAnchorInTheMap()
	{
		yield return QaScene.LoadWithDialogue();

		MapDecoPlacer deco = Object.FindFirstObjectByType<MapDecoPlacer>();
		Assert.IsNotNull(deco, "씬에 MapDecoPlacer가 없다");

		int level = Managers.Game.CurrentLevel;
		List<string> missing = new List<string>();
		int expected = 0;

		foreach (DialogueTrigger trigger in DialogueTable.Book.triggers)
		{
			if (trigger.level != level)
				continue;

			expected++;

			Vector3 where;
			if (deco.TryClosestToRoute(trigger.anchor, out where) == false)
				missing.Add($"{trigger.id} → {trigger.anchor}");
		}

		Assert.Greater(expected, 0, $"L{level}에 걸릴 대사가 하나도 없다");

		// 앵커가 사라지면 대사는 조용히 안 뜬다. 구운 맵이 장식을 지우면 여기서 걸린다.
		Assert.IsEmpty(missing,
			$"L{level}: 대사를 걸 에셋이 맵에 없다:\n{string.Join("\n", missing)}");
	}

	[UnityTest]
	public IEnumerator DirectorArmsEveryTriggerForThisLevel()
	{
		yield return QaScene.LoadWithDialogue();

		DialogueDirector director = Object.FindFirstObjectByType<DialogueDirector>();
		Assert.IsNotNull(director, "씬에 DialogueDirector가 없다");

		int level = Managers.Game.CurrentLevel;
		int expected = 0;

		foreach (DialogueTrigger trigger in DialogueTable.Book.triggers)
		{
			if (trigger.level == level)
				expected++;
		}

		Assert.AreEqual(expected, director.Count,
			$"L{level} 대사 {expected}개 중 {director.Count}개만 걸렸다");
	}

	[UnityTest]
	public IEnumerator WalkingUpToAnAssetMakesItSpeak()
	{
		yield return QaScene.LoadWithDialogue();

		MapDecoPlacer deco = Object.FindFirstObjectByType<MapDecoPlacer>();
		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		Assert.IsNotNull(deco);
		Assert.IsNotNull(player);

		DialogueTrigger first = null;
		foreach (DialogueTrigger trigger in DialogueTable.Book.triggers)
		{
			if (trigger.level == Managers.Game.CurrentLevel && trigger.requireAll == false
				&& trigger.requireCollected == 0)
			{
				first = trigger;
				break;
			}
		}

		Assert.IsNotNull(first, "조건 없는 대사가 하나는 있어야 한다");

		Vector3 anchor;
		Assert.IsTrue(deco.TryClosestToRoute(first.anchor, out anchor));

		UI_Dialogue.Clear();
		player.Teleport(anchor);

		for (int i = 0; i < 5; i++)
			yield return null;

		Assert.IsTrue(UI_Dialogue.IsShowing,
			$"{first.id}: {first.anchor} 앞에 섰는데 아무 말도 없다");

		// 읽는 동안 게임은 완전히 멈춘다.
		Assert.AreEqual(0.0f, Time.timeScale, 0.001f, "대사 중에 게임이 계속 돈다");

		UI_Dialogue.Clear();
	}

	[UnityTest]
	public IEnumerator AlreadyReadLinesDoNotComeBack()
	{
		yield return QaScene.LoadWithDialogue();

		int level = Managers.Game.CurrentLevel;

		foreach (DialogueTrigger trigger in DialogueTable.Book.triggers)
		{
			if (trigger.level == level && trigger.once)
				DialogueMemory.MarkSeen(trigger.id);
		}

		// 방금 표시한 기억을 지우면 안 되므로 잊지 않는 쪽으로 다시 연다.
		yield return QaScene.Load(QaScene.DefaultSeed, true);

		DialogueDirector director = Object.FindFirstObjectByType<DialogueDirector>();
		Assert.IsNotNull(director);
		Assert.AreEqual(0, director.Count,
			"이미 읽은 대사가 다시 걸렸다 — 재시작마다 같은 문단을 읽히면 아무도 안 읽는다");
	}
}
