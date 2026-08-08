using System.Collections;
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
	public IEnumerator YokaiFitsWhereverThePlayerFits()
	{
		yield return QaScene.Load();

		MaskYokai yokai = Wake();

		CircleCollider2D body = yokai.GetComponent<CircleCollider2D>();
		Assert.IsNotNull(body, "요괴에 몸통 콜라이더가 없다");

		// 통행 판정은 플레이어 발치 상자로 굽는다. 요괴가 그보다 크면
		// 길찾기는 통과한다고 하고 물리는 막아서 좁은 데서 낀다.
		Assert.LessOrEqual(body.radius, MapCoord.ActorHalfHeight + 0.001f,
			$"요괴 반지름 {body.radius}가 액터 발자국({MapCoord.ActorHalfHeight})보다 크다");
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
