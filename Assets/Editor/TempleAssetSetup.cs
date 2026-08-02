using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TempleAssetSetup
{
	const string TileArtDir = "Assets/Art/Temple/Tiles";
	const string ObjectArtDir = "Assets/Resources/Art/Objects";
	const string TileAssetDir = "Assets/Resources/Palette/Temple";

	const int PixelsPerUnit = 32;

	static readonly Regex TileIdPattern = new Regex(@"^tile_(\d+)_");

	[MenuItem("LampLight/Set Up Temple Assets (v3.2)")]
	public static void Run()
	{
		int tiles = ApplyImportSettings(TileArtDir);
		int objects = ApplyImportSettings(ObjectArtDir);

		AssetDatabase.Refresh();

		int created = BuildTileAssets();

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log($"TempleAssetSetup: 타일 {tiles}장 · 오브젝트 {objects}장 임포트 설정, 타일 에셋 {created}개 생성");
	}

	static int ApplyImportSettings(string dir)
	{
		if (Directory.Exists(dir) == false)
		{
			Debug.LogError($"TempleAssetSetup: {dir} 없음");
			return 0;
		}

		int applied = 0;

		foreach (string path in Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories))
		{
			TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
			if (importer == null)
				continue;

			importer.textureType = TextureImporterType.Sprite;
			importer.spriteImportMode = SpriteImportMode.Single;
			importer.spritePixelsPerUnit = PixelsPerUnit;
			importer.filterMode = FilterMode.Point;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.alphaIsTransparency = true;
			importer.mipmapEnabled = false;
			importer.wrapMode = TextureWrapMode.Clamp;

			TextureImporterSettings settings = new TextureImporterSettings();
			importer.ReadTextureSettings(settings);
			settings.spriteAlignment = path.Contains("/door/")
				? (int)SpriteAlignment.BottomCenter
				: (int)SpriteAlignment.Center;
			importer.SetTextureSettings(settings);

			importer.SaveAndReimport();
			applied++;
		}

		return applied;
	}

	static int BuildTileAssets()
	{
		Directory.CreateDirectory(TileAssetDir);

		TempleManifest.Invalidate();

		TempleCatalog catalog = TempleManifest.Catalog;
		if (catalog == null)
		{
			Debug.LogError("TempleAssetSetup: temple_catalog.json 로드 실패");
			return 0;
		}

		int created = 0;

		foreach (string path in Directory.GetFiles(TileArtDir, "*.png", SearchOption.TopDirectoryOnly))
		{
			Match match = TileIdPattern.Match(Path.GetFileName(path));
			if (match.Success == false)
				continue;

			int tileId = int.Parse(match.Groups[1].Value);

			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
			if (sprite == null)
			{
				Debug.LogError($"TempleAssetSetup: {path} 에서 스프라이트를 못 읽었다");
				continue;
			}

			TempleTile info = catalog.Tile(tileId);
			if (info == null)
			{
				Debug.LogWarning($"TempleAssetSetup: tileid {tileId} 이 매니페스트에 없다 — 건너뛴다");
				continue;
			}

			string assetPath = $"{TileAssetDir}/{TempleManifest.TileAssetName(tileId)}.asset";

			Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
			bool isNew = tile == null;

			if (isNew)
				tile = ScriptableObject.CreateInstance<Tile>();

			tile.sprite = sprite;
			tile.colliderType = info.walkable ? Tile.ColliderType.None : Tile.ColliderType.Grid;

			if (isNew)
				AssetDatabase.CreateAsset(tile, assetPath);
			else
				EditorUtility.SetDirty(tile);

			created++;
		}

		return created;
	}
}
