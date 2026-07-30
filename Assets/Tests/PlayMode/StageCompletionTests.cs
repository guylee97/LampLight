using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class StageCompletionTests
{
	const float WaypointRadius = 0.45f;
	const float TargetRadius = 0.7f;
	const float SecondsPerTile = 0.9f;
	const float LegSlack = 4.0f;

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

			yield return bot.Tap(Key.E);
			yield return null;

			Assert.IsTrue(artifact.IsCollected, $"유물 {artifact.PointName} 앞에서 수집에 실패했다");
		}

		Assert.AreEqual(progress.Required, progress.Collected, "유물을 다 모으지 못했다");
		Assert.IsTrue(placer.ExitDoor.IsOpen, "유물을 다 모았는데 출구가 열리지 않았다");

		yield return Travel(bot, player, placer.ExitDoor.transform.position, TargetRadius);
		Assert.IsNull(bot.Failure, $"출구로 가는 길: {bot.Failure}");

		yield return bot.Tap(Key.E);
		yield return null;

		bot.Dispose();

		Assert.AreEqual(Define.StageResult.Cleared, Managers.Game.Result, "출구에서 탈출 처리가 되지 않았다");
	}

	IEnumerator Travel(PlayBot bot, PlayerController player, Vector3 target, float arriveRadius)
	{
		Vector2Int from = MapCoord.WorldToTile(player.transform.position);
		Vector2Int to = MapCoord.WorldToTile(target);

		List<Vector2Int> path = new List<Vector2Int>();
		bool routed = MapPathfinder.TryFindPath(from, to, path);
		Assert.IsTrue(routed, $"({from.x},{from.y})에서 ({to.x},{to.y})로 가는 경로가 없다");

		for (int i = 1; i < path.Count; i++)
		{
			Vector2 step = MapCoord.TileToWorld(path[i].x, path[i].y);

			yield return bot.WalkTo(step, WaypointRadius, SecondsPerTile * 4 + LegSlack);

			if (bot.Failure != null)
				yield break;
		}

		yield return bot.WalkTo(target, arriveRadius, SecondsPerTile * 4 + LegSlack);
	}

	static void DisableEnemies()
	{
		foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
			enemy.gameObject.SetActive(false);
	}
}
