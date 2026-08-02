using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WallFaceOverlapTests
{
	[UnityTest]
	public IEnumerator NoWallFaceSpriteCoversAWalkableTile([Values(1, 2, 3)] int level)
	{
		Managers.Game.SetLevel(level);
		yield return QaScene.Load();

		MapData map = Managers.Data.Map;
		Assert.IsNotNull(map);

		List<string> bad = new List<string>();

		foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(
			FindObjectsInactive.Exclude, FindObjectsSortMode.None))
		{
			if (renderer.sprite == null || renderer.enabled == false)
				continue;

			if (IsWallFace(renderer) == false)
				continue;

			Bounds bounds = renderer.bounds;

			int left = Mathf.FloorToInt(bounds.min.x + 0.01f);
			int right = Mathf.CeilToInt(bounds.max.x - 0.01f) - 1;
			int bottom = Mathf.FloorToInt(bounds.min.y + 0.01f);
			int top = Mathf.CeilToInt(bounds.max.y - 0.01f) - 1;

			for (int y = bottom; y <= top; y++)
			{
				for (int x = left; x <= right; x++)
				{
					int col = x;
					int row = map.height - 1 - y;

					if (map.Contains(col, row) == false)
						continue;

					if (MapCoord.IsWalkable(col, row))
						bad.Add($"{renderer.name}: 통행 가능 칸 ({col},{row}) 을 덮는다 "
							+ $"(월드 {renderer.transform.position})");
				}
			}
		}

		Assert.IsEmpty(bad, $"L{level} 위반 {bad.Count}건\n"
			+ string.Join("\n", bad.GetRange(0, Mathf.Min(6, bad.Count))));
	}

	static bool IsWallFace(SpriteRenderer renderer)
	{
		string name = renderer.name;

		if (name.StartsWith("walldeco"))
			return true;

		return renderer.GetComponentInParent<ExitDoor>() != null;
	}

	[UnityTest]
	public IEnumerator ExitDoorStandsOnReachableFloor([Values(1, 2, 3)] int level)
	{
		Managers.Game.SetLevel(level);
		yield return QaScene.Load();

		MapData map = Managers.Data.Map;
		MapPoint exit = map.Find(MapObjectPlacer.ExitDoorPoint);
		MapPoint start = map.Find("player_start");

		Assert.IsNotNull(exit, "출구 포인트가 없다");
		Assert.IsNotNull(start, "시작 포인트가 없다");

		Assert.IsTrue(WallFaceRules.IsDoorway(map, exit.col, exit.row,
			WallFaceRules.DoorCols, WallFaceRules.DoorRows),
			$"L{level} 출구 ({exit.col},{exit.row}) 가 문틀 규칙을 못 지킨다");

		ExitDoor door = Object.FindFirstObjectByType<ExitDoor>();
		Assert.IsNotNull(door, "출구가 씬에 없다");

		SpriteRenderer renderer = null;
		foreach (SpriteRenderer candidate in door.GetComponentsInChildren<SpriteRenderer>())
		{
			if (candidate.enabled && candidate.sprite != null)
				renderer = candidate;
		}

		Assert.IsNotNull(renderer, "출구 스프라이트가 없다");

		float doorBase = renderer.bounds.min.y;
		float standTop = map.height - exit.row;

		Assert.AreEqual(standTop, doorBase, 0.01f,
			$"L{level} 문 밑변 {doorBase} 이 서는 칸 윗변 {standTop} 과 안 맞는다");

		int reached = MapPathfinder.Distance(start, exit);
		Assert.AreNotEqual(MapPathfinder.Unreachable, reached,
			$"L{level} 시작 ({start.col},{start.row}) 에서 출구 ({exit.col},{exit.row}) 로 갈 수 없다");

		for (int dx = -1; dx <= 1; dx++)
		{
			Assert.IsTrue(MapCoord.IsWalkable(exit.col + dx, exit.row),
				$"L{level} 문 앞 ({exit.col + dx},{exit.row}) 에 설 수 없다");
		}

		Debug.Log($"L{level} 출구 ({exit.col},{exit.row}) 시작에서 {reached}타일");
	}
}
