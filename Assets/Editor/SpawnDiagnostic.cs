using System.Text;
using UnityEditor;
using UnityEngine;

public static class SpawnDiagnostic
{
	const int Samples = 12;

	[MenuItem("LampLight/Diagnose Spawn Placement")]
	public static void Run()
	{
		TempleManifest.Invalidate();

		StringBuilder sb = new StringBuilder();

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			for (int i = 0; i < Samples; i++)
			{
				int seed = 1000 + i * 137;
				bool built = Managers.Data.BuildLevelMap(level, seed);
				MapData map = Managers.Data.Map;

				if (built == false || map == null)
				{
					sb.AppendLine($"L{level} seed {seed}: 맵 없음");
					continue;
				}

				string tag = Managers.Data.LastUsedFallback ? "베이크폴백" : "생성";

				MapPoint start;
				MapPoint exit;
				bool ok = SpawnSelector.TryPickPair(map.spawns, 14, 64,
					new System.Random(seed), out start, out exit);

				if (ok == false)
					ok = SpawnSelector.TryPickFarthestPair(map.spawns, out start, out exit);

				if (ok == false)
				{
					sb.AppendLine($"L{level} seed {seed} [{tag}]: 스폰 쌍 실패");
					continue;
				}

				bool inside = map.Contains(start.col, start.row);
				bool walkable = inside && map.GetGid(map.walls, start.col, start.row) == 0;
				int reach = MapPathfinder.Distance(start, exit);

				int artifacts = 0;
				foreach (MapPoint point in map.objects)
				{
					if (point.name.StartsWith(MapObjectPlacer.ArtifactPrefix))
						artifacts++;
				}

				string flag = (inside && walkable && reach != MapPathfinder.Unreachable && artifacts > 0)
					? "OK"
					: "문제";

				sb.AppendLine($"L{level} seed {seed} [{tag}] {flag}: "
					+ $"start=({start.col},{start.row}) world=({start.x:F1},{start.y:F1}) "
					+ $"맵안={inside} 보행={walkable} 출구거리={reach} 유물={artifacts} "
					+ $"스폰수={map.spawns.Length} 맵={map.width}x{map.height}");
			}
		}

		Debug.Log("SpawnDiagnostic:\n" + sb);
	}
}
