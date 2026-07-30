using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class WallCollisionTests
{
	const float PushSeconds = 1.5f;

	static readonly (Key Key, Vector2Int Step, string Name)[] Directions =
	{
		(Key.W, new Vector2Int(0, -1), "north"),
		(Key.S, new Vector2Int(0, 1), "south"),
		(Key.A, new Vector2Int(-1, 0), "west"),
		(Key.D, new Vector2Int(1, 0), "east"),
	};

	[UnityTest]
	public IEnumerator PlayerCannotPushThroughWalls()
	{
		yield return QaScene.Load();

		MapData map = Managers.Data.Map;
		Assert.IsNotNull(map, "MapData가 로드되지 않았다");

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		Assert.IsNotNull(player, "씬에 PlayerController가 없다");

		DisableEnemies();

		PlayBot bot = new PlayBot(player.transform);
		List<string> breaches = new List<string>();

		foreach ((Key key, Vector2Int step, string name) in Directions)
		{
			Vector2Int spot = FindTileFacingWall(map, step);
			if (spot.x < 0)
				continue;

			Teleport(player, MapCoord.TileToWorld(spot.x, spot.y));
			yield return new WaitForFixedUpdate();
			yield return null;

			bot.Press(key);

			float until = Time.time + PushSeconds;
			while (Time.time < until)
				yield return null;

			bot.Release();
			yield return null;

			CapsuleCollider2D capsule = player.GetComponent<CapsuleCollider2D>();
			Vector2 center = (Vector2)player.transform.position + capsule.offset;
			Collider2D overlap = Physics2D.OverlapCapsule(center, capsule.size * 0.9f, capsule.direction, 0,
				1 << QaScene.WallLayer);

			if (overlap != null)
			{
				Vector2Int landed = MapCoord.WorldToTile(center);
				breaches.Add($"{name}: ({spot.x},{spot.y})에서 밀어 콜라이더가 벽에 파묻혔다 " +
					$"(중심 {center}, 타일 ({landed.x},{landed.y}))");
			}
		}

		bot.Dispose();

		Assert.IsEmpty(breaches, $"벽 관통 {breaches.Count}건:\n{string.Join("\n", breaches)}");
	}

	static Vector2Int FindTileFacingWall(MapData map, Vector2Int step)
	{
		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (MapCoord.IsWalkable(col, row) == false)
					continue;

				if (MapCoord.IsWalkable(col + step.x, row + step.y) == false)
					return new Vector2Int(col, row);
			}
		}

		return new Vector2Int(-1, -1);
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
}
