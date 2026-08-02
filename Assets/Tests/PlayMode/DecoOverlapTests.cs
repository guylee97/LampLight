using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DecoOverlapTests
{
	[UnityTest]
	public IEnumerator NoDecoSpriteCoversAWallTile([Values(1, 2, 3)] int level)
	{
		Managers.Game.SetLevel(level);
		yield return QaScene.Load();

		MapData map = Managers.Data.Map;
		MapDecoPlacer deco = Object.FindFirstObjectByType<MapDecoPlacer>();
		Assert.IsNotNull(deco, "데코 배치기가 없다");

		List<string> bad = new List<string>();

		foreach (SpriteRenderer renderer in deco.GetComponentsInChildren<SpriteRenderer>())
		{
			if (renderer.sprite == null)
				continue;

			if (renderer.name.StartsWith("walldeco"))
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

					if (MapCoord.IsWalkable(col, row) == false)
						bad.Add($"{renderer.name}: 벽 칸 ({col},{row}) 을 덮는다 "
							+ $"(월드 {renderer.transform.position})");
				}
			}
		}

		Assert.IsEmpty(bad, $"L{level} 위반 {bad.Count}건\n"
			+ string.Join("\n", bad.GetRange(0, Mathf.Min(6, bad.Count))));
	}
}
