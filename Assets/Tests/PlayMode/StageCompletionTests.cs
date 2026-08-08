using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class StageCompletionTests
{
	const float WaypointRadius = 0.2f;
	const float TargetRadius = 0.7f;
	const float SecondsPerTile = 0.9f;
	const float LegSlack = 4.0f;
	const float TravelBudget = 40.0f;
	const float BotSlowdown = 3.0f;

	[UnityTest]
	[Timeout(600000)]
	public IEnumerator BotCollectsEveryArtifactAndEscapes()
	{
		yield return QaScene.Load();

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		MapObjectPlacer placer = Object.FindFirstObjectByType<MapObjectPlacer>();
		StageProgress progress = Object.FindFirstObjectByType<StageProgress>();

		Assert.IsNotNull(player, "씬에 PlayerController가 없다");
		Assert.IsNotNull(placer, "씬에 MapObjectPlacer가 없다");
		Assert.IsNotNull(progress, "씬에 StageProgress가 없다");

		DisableEnemies();
		float lampBudget = HoldTheLamp(player);
		float startedAt = Time.unscaledTime;

		PlayBot bot = new PlayBot(player.transform);

		List<Artifact> artifacts = new List<Artifact>(placer.Artifacts);
		foreach (Artifact artifact in artifacts)
		{
			Vector3 target = artifact.transform.position;

			yield return Travel(bot, player, target, TargetRadius);
			Assert.IsNull(bot.Failure, $"유물 {artifact.PointName}으로 가는 길: {bot.Failure}");

			if (artifact.HoldSeconds > 0.0f)
				yield return bot.HoldKey(Key.E, artifact.HoldSeconds);
			else
				yield return bot.Tap(Key.E);

			yield return null;

			Assert.IsTrue(artifact.IsCollected, $"유물 {artifact.PointName} 앞에서 수집에 실패했다");
		}

		Assert.GreaterOrEqual(progress.Collected, progress.Required, "유물을 다 모으지 못했다");
		Altar altar = placer.Altar;
		Assert.IsNotNull(altar, "제단이 배치되지 않았다");
		Assert.IsTrue(altar.CanInteract, "유물을 다 모았는데 제단에 올릴 수 없다");

		yield return Travel(bot, player, altar.transform.position, TargetRadius);
		Assert.IsNull(bot.Failure, $"제단으로 가는 길: {bot.Failure}");

		// 클리어하면 다음 전각을 바로 불러오면서 BeginStage 가 Result 를 None 으로 되돌린다.
		// 지나간 값을 나중에 읽으면 안 되므로 그 순간을 붙잡는다.
		Define.StageResult outcome = Define.StageResult.None;
		System.Action<Define.StageResult> capture = r => outcome = r;
		Managers.Game.OnStageEnded += capture;

		float sealDeadline = Time.unscaledTime + 30.0f;
		while (outcome == Define.StageResult.None && Time.unscaledTime < sealDeadline)
		{
			yield return bot.HoldKey(Key.E, altar.HoldSeconds + 0.2f);
			yield return null;
		}

		Managers.Game.OnStageEnded -= capture;
		bot.Dispose();

		Assert.AreEqual(Define.StageResult.Cleared, outcome, "제단에서 봉인 처리가 되지 않았다");

		AltarOfferings laid = altar.GetComponent<AltarOfferings>();
		Assert.IsNotNull(laid, "제단에 AltarOfferings가 없다");
		Assert.AreEqual(progress.Required, laid.Count,
			"올린 공양물이 제단 앞에 그만큼 놓여 있어야 한다 — 숫자만 오르면 화면에서 안 읽힌다");

		// 올린 공양물은 눈에만 남는다. 콜라이더가 붙는 순간 PlayerInteractor 의
		// OverlapCircle 에 걸려 제단 앞에서 도로 주울 수 있게 된다.
		foreach (Collider2D body in laid.GetComponentsInChildren<Collider2D>(true))
		{
			Assert.AreSame(altar.gameObject, body.gameObject,
				$"{body.gameObject.name}: 올린 공양물에 콜라이더가 붙었다 — 다시 주울 수 있게 된다");
		}

		foreach (Artifact artifact in placer.Artifacts)
		{
			Assert.IsFalse(artifact.CanInteract,
				$"{artifact.PointName}: 이미 바친 공양물을 다시 주울 수 있다");
			Assert.IsFalse(artifact.gameObject.activeInHierarchy,
				$"{artifact.PointName}: 바친 공양물이 아직 맵에 남아 있다");
		}

		float elapsed = Time.time - startedAt;
		Assert.Less(elapsed, lampBudget * BotSlowdown,
			$"봇이 한 바퀴 도는 데 {elapsed:0.0}초 걸렸다 — 등불 {lampBudget:0}초의 "
			+ $"{BotSlowdown}배를 넘으면 사람이 해도 시간이 모자란다");
	}

	IEnumerator Travel(PlayBot bot, PlayerController player, Vector3 target, float arriveRadius)
	{
		List<Vector2Int> path = new List<Vector2Int>();
		float deadline = Time.unscaledTime + TravelBudget;

		while (Time.unscaledTime < deadline)
		{
			// 유물을 줍거나 전각이 바뀌면 대사가 떠서 게임이 멈춘다. 사람이 넘기듯 치운다.
			if (UI_Dialogue.IsShowing)
				UI_Dialogue.Clear();

			if (Managers.Game.Result != Define.StageResult.None)
			{
				bot.AxisLocked = false;
				yield break;
			}

			Vector2 here = player.transform.position;

			if (Vector2.Distance(here, target) <= arriveRadius)
			{
				bot.AxisLocked = false;
				yield break;
			}

			Vector2Int from = MapCoord.WorldToTile(here);
			Vector2Int to = MapCoord.WorldToTile(target);

			if (from == to)
			{
				bot.AxisLocked = false;
				yield return bot.WalkTo(target, arriveRadius, SecondsPerTile * 4 + LegSlack);
				yield break;
			}

			path.Clear();
			bool routed = MapPathfinder.TryFindPath(from, to, path);
			Assert.IsTrue(routed, $"({from.x},{from.y})에서 ({to.x},{to.y})로 가는 경로가 없다");

			Vector2Int next = path.Count > 1 ? path[1] : to;

			bot.AxisLocked = true;
			yield return bot.WalkTo(MapCoord.TileToWorld(next.x, next.y), WaypointRadius,
				SecondsPerTile * 4 + LegSlack);
			bot.AxisLocked = false;

			if (bot.Failure != null)
				yield break;
		}

		bot.Failure = $"{TravelBudget:0}초 안에 {target}에 도달하지 못했다 (마지막 위치 {player.transform.position})";
	}

	static void DisableEnemies()
	{
		foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
			enemy.gameObject.SetActive(false);
	}

	/// 봇은 사람보다 훨씬 느리게 걷는다. 등불을 시간에서 떼어내되 원래 예산은 돌려준다.
	static float HoldTheLamp(PlayerController player)
	{
		float budget = LevelTable.Get(Managers.Game.CurrentLevel).LampSeconds;

		if (player != null && player.Lamp != null)
			player.Lamp.SetMaxDuration(budget * BotSlowdown * 2.0f);

		return budget;
	}
}
