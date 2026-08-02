using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MapPreviewExport
{
	const string DefaultOutDir = "QAReports/MapPreview";
	const int DefaultSeed = 20260801;

	[Serializable]
	class DecoDump
	{
		public string key;
		public float tileX;
		public float tileY;
	}

	[Serializable]
	class PreviewDump
	{
		public int level;
		public int seed;
		public MapData map;
		public DecoDump[] deco;
	}

	[MenuItem("LampLight/Export Map Preview")]
	public static void Run()
	{
		string outDir = Arg("-previewOut", DefaultOutDir);
		int seed = ParseInt(Arg("-previewSeed", DefaultSeed.ToString()), DefaultSeed);

		Directory.CreateDirectory(outDir);

		TempleManifest.Invalidate();
		SetPieceCatalog.Invalidate();

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			int used;
			MapData map = MapGenerator.Generate(level, seed, out used);

			if (map == null)
			{
				Debug.LogError($"MapPreviewExport: L{level} 생성 실패");
				continue;
			}

			List<DecoPlacement> plan = MapDecoPlan.Build(map, used, level);

			DecoDump[] deco = new DecoDump[plan.Count];
			for (int i = 0; i < plan.Count; i++)
			{
				deco[i] = new DecoDump
				{
					key = plan[i].Key,
					tileX = plan[i].TileX,
					tileY = plan[i].TileY,
				};
			}

			PreviewDump dump = new PreviewDump
			{
				level = level,
				seed = used,
				map = map,
				deco = deco,
			};

			string path = Path.Combine(outDir, $"preview_l{level}.json");
			File.WriteAllText(path, JsonUtility.ToJson(dump));

			Debug.Log($"MapPreviewExport: L{level} seed={used} 방 {map.rooms.Length}개 "
				+ $"장식 {plan.Count}개 -> {path}");
		}

		AssetDatabase.Refresh();
	}

	static string Arg(string name, string fallback)
	{
		string[] args = Environment.GetCommandLineArgs();

		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] == name)
				return args[i + 1];
		}

		return fallback;
	}

	static int ParseInt(string raw, int fallback)
	{
		int value;
		return int.TryParse(raw, out value) ? value : fallback;
	}
}
