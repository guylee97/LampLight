using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class YokaiMovementTests
{
	const float WatchSeconds = 4.0f;
	const float MinTravel = 1.0f;

	/// 요괴는 씬이 열릴 때 없다. 움직임을 보려면 먼저 깨워야 한다.
	static MaskYokai Wake()
	{
		EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
		Assert.IsNotNull(spawner, "씬에 EnemySpawner가 없다");

		SpawnSelector selector = Object.FindFirstObjectByType<SpawnSelector>();
		Assert.IsNotNull(selector, "씬에 SpawnSelector가 없다");

		spawner.Spawn(Managers.Game.Level, selector.PlayerStart, new System.Random(4231));

		MaskYokai yokai = Object.FindFirstObjectByType<MaskYokai>();
		Assert.IsNotNull(yokai, "요괴가 스폰되지 않았다");
		return yokai;
	}

	[UnityTest]
	public IEnumerator NoYokaiStandsInTheTempleAtOpening()
	{
		yield return QaScene.Load();

		Assert.IsNull(Object.FindFirstObjectByType<MaskYokai>(),
			"신전은 조용해야 한다 — 요괴는 공양물을 건드린 뒤에 깨어난다");
	}

	[UnityTest]
	public IEnumerator EveryPatrolTargetCanActuallyBeReached()
	{
		yield return QaScene.Load();

		MaskYokai yokai = Wake();
		Vector2Int from = MapCoord.WorldToTile(yokai.transform.position);
		int[] field = MapPathfinder.DistanceField(from.x, from.y);

		MapData map = Managers.Data.Map;
		Assert.IsNotNull(map);

		List<string> unreachable = new List<string>();

		foreach (Vector2 target in yokai.PatrolRoute)
		{
			Vector2Int tile = MapCoord.WorldToTile(target);

			if (MapPathfinder.Sample(field, tile.x, tile.y) == MapPathfinder.Unreachable)
				unreachable.Add($"({tile.x},{tile.y})");
		}

		// 못 가는 곳을 목표로 잡으면 길찾기가 실패하고 그쪽 벽에 붙어 선다.
		Assert.IsEmpty(unreachable,
			$"순찰 목표에 닿을 수 없다: {string.Join(", ", unreachable)}");
	}

	[UnityTest]
	public IEnumerator YokaiFitsWhereverThePlayerFits()
	{
		yield return QaScene.Load();

		MaskYokai yokai = Wake();

		CapsuleCollider2D body = yokai.GetComponent<CapsuleCollider2D>();
		Assert.IsNotNull(body, "요괴에 발치 콜라이더가 없다");

		// 플레이어와 같은 발자국이어야 한다. 크면 플레이어가 지나는 틈에 끼고,
		// 작으면 판정이 막힘이라 부르는 칸에 들어가 길찾기가 성립하지 않는다.
		Assert.AreEqual(YokaiFactory.ActorFootSize.x, body.size.x, 0.001f,
			"요괴 발자국 너비가 플레이어와 다르다");
		Assert.AreEqual(YokaiFactory.ActorFootSize.y, body.size.y, 0.001f,
			"요괴 발자국 높이가 플레이어와 다르다");
		Assert.AreEqual(MapCoord.ActorFootOffset, body.offset.y, 0.001f,
			"요괴 충돌이 발이 아니라 몸통 한가운데에 있다");
	}

	[UnityTest]
	public IEnumerator YokaiSpawnsAndActuallyMoves()
	{
		yield return QaScene.Load();

		MaskYokai yokai = Wake();

		Vector2 start = yokai.transform.position;
		Vector2Int tile = MapCoord.WorldToTile(start);

		Assert.IsTrue(MapCoord.IsPassable(tile.x, tile.y),
			$"요괴가 통과 불가 칸 ({tile.x},{tile.y}) 에서 시작한다");

		float travelled = 0.0f;
		Vector2 previous = start;
		StringBuilder trail = new StringBuilder();
		float deadline = Time.time + WatchSeconds;
		float nextSample = 0.0f;

		while (Time.time < deadline)
		{
			Vector2 now = yokai.transform.position;
			travelled += Vector2.Distance(previous, now);
			previous = now;

			if (Time.time >= nextSample)
			{
				nextSample = Time.time + 0.5f;
				trail.Append($"[{yokai.State} {now.x:F2},{now.y:F2}] ");
			}

			yield return null;
		}

		Assert.GreaterOrEqual(travelled, MinTravel,
			$"요괴가 {WatchSeconds}초 동안 {travelled:F2}타일만 움직였다. 궤적: {trail}");
	}

	[UnityTest]
	public IEnumerator YokaiLeavesItsStartingRoom()
	{
		yield return QaScene.Load();

		MaskYokai yokai = Wake();

		Vector2 start = yokai.transform.position;
		float farthest = 0.0f;
		float deadline = Time.time + 8.0f;

		while (Time.time < deadline)
		{
			farthest = Mathf.Max(farthest, Vector2.Distance(start, yokai.transform.position));
			yield return null;
		}

		Assert.GreaterOrEqual(farthest, 3.0f,
			$"요괴가 8초 동안 시작점에서 최대 {farthest:F2}타일밖에 못 벗어났다 "
			+ $"(상태 {yokai.State})");
	}
}
