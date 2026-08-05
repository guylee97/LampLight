using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class TilemapAlignmentTests
{
	[UnityTest]
	public IEnumerator ColliderGeometryMatchesMapData()
	{
		yield return QaScene.Load();

		MapData map = Managers.Data.Map;
		Assert.IsNotNull(map);

		int mask = 1 << QaScene.WallLayer;
		List<string> ghosts = new List<string>();
		List<string> holes = new List<string>();

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				Vector3 centre = MapCoord.TileToWorld(col, row);
				bool solid = Physics2D.OverlapPoint(centre, mask) != null;
				bool blockedInData = MapCoord.IsPassable(col, row) == false;

				if (blockedInData && solid == false)
					holes.Add($"({col},{row}) world={centre} 데이터는 차단인데 콜라이더 없음");

				if (blockedInData == false && solid)
					ghosts.Add($"({col},{row}) world={centre} 데이터는 통과인데 콜라이더 있음");
			}
		}

		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"유령 콜라이더 {ghosts.Count}칸 / 구멍 {holes.Count}칸");

		for (int i = 0; i < ghosts.Count && i < 5; i++)
			sb.AppendLine("  " + ghosts[i]);

		for (int i = 0; i < holes.Count && i < 5; i++)
			sb.AppendLine("  " + holes[i]);

		Assert.IsTrue(ghosts.Count == 0 && holes.Count == 0, sb.ToString());
	}

	[UnityTest]
	public IEnumerator PaintedTilesMatchMapData()
	{
		yield return QaScene.Load();

		MapData map = Managers.Data.Map;
		Tilemap wall = null;

		foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include,
			FindObjectsSortMode.None))
		{
			if (tilemap.name == "Wall")
				wall = tilemap;
		}

		Assert.IsNotNull(wall, "Wall 타일맵이 없다");

		List<string> bad = new List<string>();

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				bool wallInData = map.GetGid(map.walls, col, row) != 0;
				Vector3Int cell = new Vector3Int(col, map.height - 1 - row, 0);
				bool painted = wall.GetTile(cell) != null;

				if (wallInData != painted)
					bad.Add($"({col},{row}) cell={cell} 데이터 {(wallInData ? "벽" : "바닥")} / "
						+ $"그려진 것 {(painted ? "벽" : "없음")}");
			}
		}

		Assert.IsEmpty(bad, $"불일치 {bad.Count}칸\n" + string.Join("\n", bad.GetRange(0, Mathf.Min(6, bad.Count))));
	}

	[UnityTest]
	public IEnumerator TilemapTransformHasNoOffset()
	{
		yield return QaScene.Load();

		foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include,
			FindObjectsSortMode.None))
		{
			Vector3 position = tilemap.transform.position;
			Assert.AreEqual(0.0f, position.x, 0.001f, $"{tilemap.name} x 오프셋 {position.x}");
			Assert.AreEqual(0.0f, position.y, 0.001f, $"{tilemap.name} y 오프셋 {position.y}");

			Vector3 anchor = tilemap.tileAnchor;
			Assert.AreEqual(0.5f, anchor.x, 0.001f, $"{tilemap.name} tileAnchor.x {anchor.x}");
			Assert.AreEqual(0.5f, anchor.y, 0.001f, $"{tilemap.name} tileAnchor.y {anchor.y}");

			Grid grid = tilemap.layoutGrid;
			if (grid != null)
			{
				Assert.AreEqual(1.0f, grid.cellSize.x, 0.001f, "grid cellSize.x");
				Assert.AreEqual(1.0f, grid.cellSize.y, 0.001f, "grid cellSize.y");
				Assert.AreEqual(0.0f, grid.transform.position.x, 0.001f, "grid x");
				Assert.AreEqual(0.0f, grid.transform.position.y, 0.001f, "grid y");
			}
		}
	}
}
