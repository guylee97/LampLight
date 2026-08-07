using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class YokaiMovementTests
{
	const float WatchSeconds = 4.0f;
	const float MinTravel = 1.0f;

	[UnityTest]
	public IEnumerator YokaiSpawnsAndActuallyMoves()
	{
		yield return QaScene.Load();

		MaskYokai yokai = Object.FindFirstObjectByType<MaskYokai>();
		Assert.IsNotNull(yokai, "요괴가 스폰되지 않았다");

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

		MaskYokai yokai = Object.FindFirstObjectByType<MaskYokai>();
		Assert.IsNotNull(yokai);

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
