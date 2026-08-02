using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

public class MapGalleryExportTests
{
	const int Samples = 24;

	[Test]
	public void ExportGallery()
	{
		StringBuilder sb = new StringBuilder();

		for (int level = 1; level <= 3; level++)
		{
			for (int i = 0; i < Samples; i++)
			{
				int used;
				MapData map = MapGenerator.Generate(level, 4200 + i, out used);
				Assert.IsNotNull(map);

				MapPoint start = map.Find("player_start");
				MapPoint exit = map.Find(MapObjectPlacer.ExitDoorPoint);

				sb.AppendLine($"#MAP level={level} seed={used} w={map.width} h={map.height} "
					+ $"rooms={(map.rooms == null ? 0 : map.rooms.Length)} "
					+ $"start={start.col},{start.row} exit={exit.col},{exit.row}");

				foreach (MapPoint point in map.objects)
				{
					if (point.name.StartsWith("artifact"))
						sb.AppendLine($"#ART {point.col},{point.row}");
				}

				for (int row = 0; row < map.height; row++)
				{
					StringBuilder line = new StringBuilder();
					for (int col = 0; col < map.width; col++)
						line.Append(map.GetGid(map.walls, col, row) == 0 ? '.' : '#');

					sb.AppendLine(line.ToString());
				}
			}
		}

		string dir = Path.Combine(Application.dataPath, "..", "QAReports");
		Directory.CreateDirectory(dir);
		File.WriteAllText(Path.Combine(dir, "map_gallery.txt"), sb.ToString());
		Debug.Log($"MapGallery: {Samples * 3}개 맵 덤프");
	}
}
