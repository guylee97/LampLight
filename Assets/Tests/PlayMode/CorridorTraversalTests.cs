using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CorridorTraversalTests
{
	const float ArriveRadius = 0.35f;
	const float SecondsPerTile = 0.9f;
	const float TimeoutSlack = 3.0f;

	struct Corridor
	{
		public Vector2Int From;
		public Vector2Int To;
		public bool Horizontal;

		public int Length
		{
			get { return Horizontal ? Mathf.Abs(To.x - From.x) : Mathf.Abs(To.y - From.y); }
		}
	}

	[UnityTest]
	public IEnumerator BotWalksEveryNarrowCorridorEndToEnd()
	{
		yield return QaScene.Load();

		MapData map = Managers.Data.Map;
		Assert.IsNotNull(map, "MapData가 로드되지 않았다");

		DisableEnemies();

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		Assert.IsNotNull(player, "씬에 PlayerController가 없다");

		List<Corridor> corridors = FindCorridors(map);

		if (corridors.Count == 0)
		{
			Debug.Log("CorridorTraversal: 1타일 병목 없음 — 복도 폭 2타일 규칙 충족");
			yield break;
		}

		PlayBot bot = new PlayBot(player.transform);
		List<string> failures = new List<string>();

		foreach (Corridor corridor in corridors)
		{
			// 시간이 지나면 요괴가 스스로 깨어난다. 복도 통과만 보는 시험이라 다시 재운다.
			DisableEnemies();

			yield return RunCorridor(bot, player, corridor, failures);
			yield return RunCorridor(bot, player, Reverse(corridor), failures);
		}

		bot.Dispose();

		Debug.Log($"CorridorTraversal: {corridors.Count * 2} runs, {failures.Count} failed");

		if (failures.Count > 0)
		{
			string report = QaScene.WriteReport("corridor_traversal.txt", string.Join("\n", failures));
			Assert.Fail($"1타일 복도 주행 {failures.Count}건 실패. 리포트: {report}\n{string.Join("\n", failures)}");
		}
	}

	IEnumerator RunCorridor(PlayBot bot, PlayerController player, Corridor corridor, List<string> failures)
	{
		Vector2 start = MapCoord.TileToWorld(corridor.From.x, corridor.From.y);
		Vector2 goal = MapCoord.TileToWorld(corridor.To.x, corridor.To.y);

		Teleport(player, start);
		yield return new WaitForFixedUpdate();
		yield return null;

		float timeout = corridor.Length * SecondsPerTile + TimeoutSlack;
		yield return bot.WalkTo(goal, ArriveRadius, timeout);

		if (bot.Arrived)
			yield break;

		failures.Add($"({corridor.From.x},{corridor.From.y}) -> ({corridor.To.x},{corridor.To.y}): {bot.Failure}");
	}

	static void Teleport(PlayerController player, Vector2 position)
	{
		Rigidbody2D body = player.GetComponent<Rigidbody2D>();

		player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);

		if (body != null)
		{
			body.position = position;
			body.linearVelocity = Vector2.zero;
		}

		Physics2D.SyncTransforms();
	}

	static void DisableEnemies()
	{
		foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
			enemy.gameObject.SetActive(false);
	}

	static Corridor Reverse(Corridor corridor)
	{
		Corridor flipped;
		flipped.From = corridor.To;
		flipped.To = corridor.From;
		flipped.Horizontal = corridor.Horizontal;
		return flipped;
	}

	static List<Corridor> FindCorridors(MapData map)
	{
		List<Corridor> corridors = new List<Corridor>();
		HashSet<Vector2Int> seen = new HashSet<Vector2Int>();

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (MapCoord.IsWalkable(col, row) == false)
					continue;

				bool horizontal = Blocked(col, row - 1) && Blocked(col, row + 1);
				bool vertical = Blocked(col - 1, row) && Blocked(col + 1, row);

				if (horizontal == false && vertical == false)
					continue;

				Vector2Int tile = new Vector2Int(col, row);
				if (seen.Contains(tile))
					continue;

				Corridor corridor = Extend(tile, horizontal, seen);
				if (corridor.Length > 0)
					corridors.Add(corridor);
			}
		}

		return corridors;
	}

	static Corridor Extend(Vector2Int tile, bool horizontal, HashSet<Vector2Int> seen)
	{
		Vector2Int step = horizontal ? new Vector2Int(1, 0) : new Vector2Int(0, 1);

		Vector2Int low = tile;
		Vector2Int high = tile;

		while (IsCorridorCell(low - step, horizontal))
			low -= step;

		while (IsCorridorCell(high + step, horizontal))
			high += step;

		for (Vector2Int walk = low; walk != high + step; walk += step)
			seen.Add(walk);

		Vector2Int from = MapCoord.IsWalkable(low.x - step.x, low.y - step.y) ? low - step : low;
		Vector2Int to = MapCoord.IsWalkable(high.x + step.x, high.y + step.y) ? high + step : high;

		Corridor corridor;
		corridor.From = from;
		corridor.To = to;
		corridor.Horizontal = horizontal;
		return corridor;
	}

	static bool IsCorridorCell(Vector2Int tile, bool horizontal)
	{
		if (MapCoord.IsWalkable(tile.x, tile.y) == false)
			return false;

		return horizontal
			? Blocked(tile.x, tile.y - 1) && Blocked(tile.x, tile.y + 1)
			: Blocked(tile.x - 1, tile.y) && Blocked(tile.x + 1, tile.y);
	}

	static bool Blocked(int col, int row)
	{
		return MapCoord.IsWalkable(col, row) == false;
	}
}
