using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class CharacterAssetSetup
{
	const string ResourceRoot = "Assets/Resources/Art/Characters";
	const int PixelsPerUnit = 32;

	[MenuItem("LampLight/Slice Character Sheets")]
	public static void Run()
	{
		CharacterCatalog.Invalidate();

		CharacterCatalogData data = CharacterCatalog.Data;
		if (data == null)
			return;

		int sheets = 0;
		int sprites = 0;

		foreach (CharacterSpec spec in data.characters)
		{
			foreach (CharacterState state in spec.states)
			{
				int made = Slice(spec, state);
				if (made > 0)
				{
					sheets++;
					sprites += made;
				}
			}
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log($"CharacterAssetSetup: 시트 {sheets}장에서 스프라이트 {sprites}개 슬라이스");
	}

	static int Slice(CharacterSpec spec, CharacterState state)
	{
		string path = $"{ResourceRoot}/{spec.key}/{System.IO.Path.GetFileName(state.resource)}.png";

		TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
		if (importer == null)
		{
			Debug.LogError($"CharacterAssetSetup: {path} 없음");
			return 0;
		}

		importer.textureType = TextureImporterType.Sprite;
		importer.spriteImportMode = SpriteImportMode.Multiple;
		importer.spritePixelsPerUnit = PixelsPerUnit;
		importer.filterMode = FilterMode.Point;
		importer.textureCompression = TextureImporterCompression.Uncompressed;
		importer.alphaIsTransparency = true;
		importer.mipmapEnabled = false;
		importer.wrapMode = TextureWrapMode.Clamp;
		importer.SaveAndReimport();

		Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		if (texture == null)
			return 0;

		SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
		factories.Init();

		ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
		provider.InitSpriteEditorDataProvider();

		List<SpriteRect> rects = new List<SpriteRect>();
		Vector2 pivot = new Vector2(spec.pivotX, spec.pivotY);

		for (int direction = 0; direction < CharacterCatalog.DirectionCount; direction++)
		{
			for (int frame = 0; frame < state.frames; frame++)
			{
				int col = state.DirectionIsRow ? frame : direction;
				int rowFromTop = state.DirectionIsRow ? direction : 0;

				if (col >= state.cols || rowFromTop >= state.rows)
					continue;

				float x = col * spec.frameWidth;
				float y = texture.height - (rowFromTop + 1) * spec.frameHeight;

				rects.Add(new SpriteRect
				{
					name = CharacterCatalog.SpriteName(spec.key, state.name,
						spec.directions[direction], frame),
					rect = new Rect(x, y, spec.frameWidth, spec.frameHeight),
					alignment = SpriteAlignment.Custom,
					pivot = pivot,
					border = Vector4.zero,
					spriteID = GUID.Generate(),
				});
			}
		}

		provider.SetSpriteRects(rects.ToArray());

		ISpriteNameFileIdDataProvider names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
		if (names != null)
		{
			List<SpriteNameFileIdPair> pairs = new List<SpriteNameFileIdPair>();
			foreach (SpriteRect rect in rects)
				pairs.Add(new SpriteNameFileIdPair(rect.name, rect.spriteID));

			names.SetNameFileIdPairs(pairs);
		}

		provider.Apply();
		importer.SaveAndReimport();

		return rects.Count;
	}
}
