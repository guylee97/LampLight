using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class SpawnPlacementTests
{
	[UnityTest]
	public IEnumerator PlayerStartsOnAWalkableTileInsideTheMap()
	{
		yield return QaScene.Load();

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		Assert.IsNotNull(player, "플레이어가 없다");

		MapData map = Managers.Data.Map;
		Assert.IsNotNull(map, "맵이 없다");

		Vector3 position = player.transform.position;
		Vector2Int tile = MapCoord.WorldToTile(position);

		Assert.IsTrue(map.Contains(tile.x, tile.y),
			$"시작 위치가 맵 밖이다 world={position} tile={tile} 맵={map.width}x{map.height}");

		Assert.IsTrue(MapCoord.IsWalkable(tile.x, tile.y),
			$"시작 위치가 벽 위다 world={position} tile={tile}");
	}

	[UnityTest]
	public IEnumerator PlayerStaysPutAfterPhysicsSettles()
	{
		yield return QaScene.Load();

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		Assert.IsNotNull(player);

		Vector3 spawned = player.transform.position;

		for (int i = 0; i < 30; i++)
			yield return new WaitForFixedUpdate();

		float drift = Vector2.Distance(spawned, player.transform.position);

		Assert.Less(drift, 1.0f,
			$"물리가 시작 위치를 {drift:F2} 유닛 밀어냈다 "
			+ $"({spawned} -> {player.transform.position})");
	}

	[UnityTest]
	public IEnumerator EveryWallTileHasACollider()
	{
		yield return QaScene.Load();

		MapData map = Managers.Data.Map;
		Assert.IsNotNull(map);

		int mask = 1 << QaScene.WallLayer;
		int checkedTiles = 0;
		int missing = 0;
		string first = null;

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (map.GetGid(map.walls, col, row) == 0)
					continue;

				checkedTiles++;

				Vector3 center = MapCoord.TileToWorld(col, row);
				if (Physics2D.OverlapPoint(center, mask) != null)
					continue;

				missing++;
				if (first == null)
					first = $"({col},{row}) world={center}";
			}
		}

		Assert.Greater(checkedTiles, 0, "벽 타일이 하나도 없다");
		Assert.AreEqual(0, missing,
			$"벽 {checkedTiles}칸 중 {missing}칸에 콜라이더가 없다. 첫 사례 {first}");
	}

	[UnityTest]
	public IEnumerator PlayerCannotEnterAWallTile()
	{
		yield return QaScene.Load();

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		MapData map = Managers.Data.Map;
		Assert.IsNotNull(player);
		Assert.IsNotNull(map);

		Rigidbody2D body = player.GetComponent<Rigidbody2D>();
		Assert.IsNotNull(body);

		Vector2Int[] steps = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
		int tested = 0;
		string breach = null;

		for (int row = 1; row < map.height - 1 && breach == null; row++)
		{
			for (int col = 1; col < map.width - 1 && breach == null; col++)
			{
				if (MapCoord.IsWalkable(col, row) == false)
					continue;

				foreach (Vector2Int step in steps)
				{
					Vector2Int wall = new Vector2Int(col + step.x, row + step.y);
					if (MapCoord.IsWalkable(wall.x, wall.y))
						continue;

					player.Teleport(MapCoord.TileToWorld(col, row));
					yield return new WaitForFixedUpdate();

					Vector3 target = MapCoord.TileToWorld(wall.x, wall.y);

					for (int i = 0; i < 40; i++)
					{
						Vector2 push = ((Vector2)(target - player.transform.position)).normalized * 0.12f;
						body.MovePosition(body.position + push);
						yield return new WaitForFixedUpdate();
					}

					Vector2Int landed = BodyTile(player);
					tested++;

					if (MapCoord.IsWalkable(landed.x, landed.y) == false)
						breach = $"({col},{row}) → 벽 ({wall.x},{wall.y}) 밀었더니 ({landed.x},{landed.y}) 로 들어감";

					break;
				}

				if (tested >= 6)
					break;
			}

			if (tested >= 6)
				break;
		}

		Assert.Greater(tested, 0, "벽에 인접한 칸을 못 찾았다");
		Assert.IsNull(breach, breach);
	}

	[UnityTest]
	public IEnumerator RealInputCannotPushPlayerIntoAWall()
	{
		yield return QaScene.Load();

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		MapData map = Managers.Data.Map;
		Assert.IsNotNull(player);
		Assert.IsNotNull(map);

		PlayBot bot = new PlayBot(player.transform);
		List<string> breaches = new List<string>();

		(Key key, Vector2Int step, string name)[] dirs =
		{
			(Key.W, new Vector2Int(0, -1), "위"),
			(Key.S, new Vector2Int(0, 1), "아래"),
			(Key.A, new Vector2Int(-1, 0), "왼쪽"),
			(Key.D, new Vector2Int(1, 0), "오른쪽"),
		};

		foreach ((Key key, Vector2Int step, string name) in dirs)
		{
			Vector2Int from = FindTileFacing(map, step);
			if (from.x < 0)
				continue;

			player.Teleport(MapCoord.TileToWorld(from.x, from.y));
			yield return new WaitForFixedUpdate();
			yield return null;

			bot.Press(key);

			float until = Time.time + 1.5f;
			while (Time.time < until)
				yield return null;

			bot.Release();
			yield return new WaitForFixedUpdate();

			Vector2Int landed = BodyTile(player);

			if (MapCoord.IsWalkable(landed.x, landed.y) == false)
			{
				breaches.Add($"{name}: ({from.x},{from.y}) 에서 밀었더니 벽 ({landed.x},{landed.y}) 로 들어갔다 "
					+ $"(몸통 {player.GetComponent<Collider2D>().bounds.center})");
			}
		}

		bot.Dispose();
		Assert.IsEmpty(breaches, string.Join("\n", breaches));
	}

	static Vector2Int BodyTile(PlayerController player)
	{
		Collider2D body = player.GetComponent<Collider2D>();
		Vector3 probe = body != null ? body.bounds.center : player.transform.position;
		return MapCoord.WorldToTile(probe);
	}

	static Vector2Int FindTileFacing(MapData map, Vector2Int step)
	{
		for (int row = 1; row < map.height - 1; row++)
		{
			for (int col = 1; col < map.width - 1; col++)
			{
				if (MapCoord.IsWalkable(col, row) == false)
					continue;

				if (MapCoord.IsWalkable(col + step.x, row + step.y) == false)
					return new Vector2Int(col, row);
			}
		}

		return new Vector2Int(-1, -1);
	}

	[UnityTest]
	public IEnumerator AltarSitsOnAWalkableTile()
	{
		yield return QaScene.Load();

		MapObjectPlacer placer = Object.FindFirstObjectByType<MapObjectPlacer>();
		Assert.IsNotNull(placer);
		Assert.IsNotNull(placer.Altar, "제단이 배치되지 않았다");

		Vector2Int tile = MapCoord.WorldToTile(placer.Altar.transform.position);

		Assert.IsTrue(MapCoord.IsWalkable(tile.x, tile.y),
			$"제단이 벽 위에 있다 tile={tile}");
	}
}
