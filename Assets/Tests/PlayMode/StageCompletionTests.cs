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
	const float TravelBudget = 120.0f;

	[UnityTest]
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

		foreach (Container container in Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
		{
			if (container.HoldsArtifact == false)
				continue;

			Vector3 spot = ApproachSpot(container.transform.position);
			yield return Travel(bot, player, spot, TargetRadius);
			Assert.IsNull(bot.Failure, $"유물이 든 상자로 가는 길: {bot.Failure}");

			if (container.HoldSeconds > 0.0f)
				yield return bot.HoldKey(Key.E, container.HoldSeconds);
			else
				yield return bot.Tap(Key.E);

			yield return null;

			Assert.IsFalse(container.HoldsArtifact,
				"상자를 열었는데 안에 있던 유물이 그대로다");
		}

		Assert.GreaterOrEqual(progress.Collected, progress.Required, "유물을 다 모으지 못했다");
		Altar altar = placer.Altar;
		Assert.IsNotNull(altar, "제단이 배치되지 않았다");
		Assert.IsTrue(altar.CanInteract, "유물을 다 모았는데 제단에 올릴 수 없다");

		yield return Travel(bot, player, altar.transform.position, TargetRadius);
		Assert.IsNull(bot.Failure, $"제단으로 가는 길: {bot.Failure}");

		while (Managers.Game.Result == Define.StageResult.None && altar.IsSealed == false)
		{
			yield return bot.HoldKey(Key.E, altar.HoldSeconds + 0.2f);
			yield return null;
		}

		bot.Dispose();

		Assert.AreEqual(Define.StageResult.Cleared, Managers.Game.Result, "제단에서 봉인 처리가 되지 않았다");
	}

	IEnumerator Travel(PlayBot bot, PlayerController player, Vector3 target, float arriveRadius)
	{
		List<Vector2Int> path = new List<Vector2Int>();
		float deadline = Time.time + TravelBudget;

		while (Time.time < deadline)
		{
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

	static Vector3 ApproachSpot(Vector3 world)
	{
		Vector2Int tile = MapCoord.WorldToTile(world);
		if (MapCoord.IsPassable(tile.x, tile.y))
			return world;

		for (int radius = 1; radius <= 3; radius++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				for (int dx = -radius; dx <= radius; dx++)
				{
					if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius)
						continue;

					if (MapCoord.IsPassable(tile.x + dx, tile.y + dy))
						return MapCoord.TileToWorld(tile.x + dx, tile.y + dy);
				}
			}
		}

		return world;
	}

	static void DisableEnemies()
	{
		foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
			enemy.gameObject.SetActive(false);
	}
}
