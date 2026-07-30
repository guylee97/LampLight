using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NavigationSweepTests
{
	const int TilePixels = 8;

	static readonly Vector2Int[] Steps =
	{
		new Vector2Int(1, 0),
		new Vector2Int(-1, 0),
		new Vector2Int(0, 1),
		new Vector2Int(0, -1),
	};

	struct BodyShape
	{
		public Vector2 Size;
		public Vector2 Offset;
		public CapsuleDirection2D Direction;
	}

	[UnityTest]
	public IEnumerator EveryWalkableTileIsStandableAndConnected()
	{
		yield return QaScene.Load();

		MapData map = Managers.Data.Map;
		Assert.IsNotNull(map, "MapData가 로드되지 않았다");

		BodyShape body = ReadPlayerShape();
		int mask = 1 << QaScene.WallLayer;

		List<Vector2Int> unstandable = new List<Vector2Int>();
		List<string> blockedEdges = new List<string>();
		HashSet<Vector2Int> edgeTiles = new HashSet<Vector2Int>();

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (MapCoord.IsWalkable(col, row) == false)
					continue;

				Vector2 center = Center(col, row, body);

				if (Physics2D.OverlapCapsule(center, body.Size, body.Direction, 0, mask) != null)
				{
					unstandable.Add(new Vector2Int(col, row));
					continue;
				}

				foreach (Vector2Int step in Steps)
				{
					int nextCol = col + step.x;
					int nextRow = row + step.y;

					if (MapCoord.IsWalkable(nextCol, nextRow) == false)
						continue;

					Vector2 target = Center(nextCol, nextRow, body);
					Vector2 delta = target - center;

					RaycastHit2D hit = Physics2D.CapsuleCast(center, body.Size, body.Direction, 0,
						delta.normalized, delta.magnitude, mask);

					if (hit.collider == null)
						continue;

					blockedEdges.Add($"({col},{row}) -> ({nextCol},{nextRow})");
					edgeTiles.Add(new Vector2Int(col, row));
					edgeTiles.Add(new Vector2Int(nextCol, nextRow));
				}
			}
		}

		string png = WriteHeatmap(map, unstandable, edgeTiles);
		string report = WriteReport(map, body, unstandable, blockedEdges, png);

		Debug.Log($"NavigationSweep: unstandable={unstandable.Count} blockedEdges={blockedEdges.Count} report={report}");

		Assert.IsEmpty(unstandable,
			$"플레이어 콜라이더가 들어가지 못하는 walkable 타일 {unstandable.Count}개. 리포트: {report}");

		Assert.IsEmpty(blockedEdges,
			$"인접한 walkable 타일 사이를 물리적으로 지나갈 수 없는 구간 {blockedEdges.Count}개. 리포트: {report}");
	}

	static BodyShape ReadPlayerShape()
	{
		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		Assert.IsNotNull(player, "씬에 PlayerController가 없다");

		CapsuleCollider2D capsule = player.GetComponent<CapsuleCollider2D>();
		Assert.IsNotNull(capsule, "플레이어에 CapsuleCollider2D가 없다");

		BodyShape shape;
		shape.Size = capsule.size;
		shape.Offset = capsule.offset;
		shape.Direction = capsule.direction;
		return shape;
	}

	static Vector2 Center(int col, int row, BodyShape body)
	{
		return (Vector2)MapCoord.TileToWorld(col, row) + body.Offset;
	}

	static string WriteHeatmap(MapData map, List<Vector2Int> unstandable, HashSet<Vector2Int> edgeTiles)
	{
		int width = map.width * TilePixels;
		int height = map.height * TilePixels;

		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		Color32[] pixels = new Color32[width * height];

		Color32 wall = new Color32(38, 38, 44, 255);
		Color32 open = new Color32(178, 182, 190, 255);
		Color32 edge = new Color32(240, 150, 40, 255);
		Color32 stuck = new Color32(220, 45, 45, 255);

		HashSet<Vector2Int> unstandableSet = new HashSet<Vector2Int>(unstandable);

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				Vector2Int tile = new Vector2Int(col, row);

				Color32 color = wall;
				if (MapCoord.IsWalkable(col, row))
					color = open;

				if (edgeTiles.Contains(tile))
					color = edge;

				if (unstandableSet.Contains(tile))
					color = stuck;

				Fill(pixels, width, height, col, row, color);
			}
		}

		texture.SetPixels32(pixels);
		texture.Apply();

		string path = QaScene.WritePng("navigation_heatmap.png", texture);
		Object.DestroyImmediate(texture);
		return path;
	}

	static void Fill(Color32[] pixels, int width, int height, int col, int row, Color32 color)
	{
		int originX = col * TilePixels;
		int originY = height - (row + 1) * TilePixels;

		for (int y = 0; y < TilePixels; y++)
		{
			for (int x = 0; x < TilePixels; x++)
				pixels[(originY + y) * width + originX + x] = color;
		}
	}

	static string WriteReport(MapData map, BodyShape body, List<Vector2Int> unstandable,
		List<string> blockedEdges, string png)
	{
		StringBuilder text = new StringBuilder();
		text.AppendLine("# Navigation sweep");
		text.AppendLine($"map: {map.width}x{map.height}");
		text.AppendLine($"body: size={body.Size} offset={body.Offset} direction={body.Direction}");
		text.AppendLine($"heatmap: {png}");
		text.AppendLine();

		text.AppendLine($"## unstandable tiles ({unstandable.Count})");
		foreach (Vector2Int tile in unstandable)
			text.AppendLine($"({tile.x},{tile.y}) world={MapCoord.TileToWorld(tile.x, tile.y)}");

		text.AppendLine();
		text.AppendLine($"## blocked edges ({blockedEdges.Count})");
		foreach (string line in blockedEdges)
			text.AppendLine(line);

		return QaScene.WriteReport("navigation_sweep.txt", text.ToString());
	}
}
