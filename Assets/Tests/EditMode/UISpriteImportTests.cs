using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class UISpriteImportTests
{
	static readonly string[] LoadedByUI =
	{
		"Art/UI/Game_Over_Screen/jumpscare_face",
		"Art/UI/Common/press_any_key",
		"Art/UI/Common/esc_title",
		"Art/UI/Game_Over_Screen/Your Dead Title",
		"Art/UI/Game_Over_Screen/wooden_planks",
		"Art/UI/Game_Over_Screen/Play Again",
		"Art/UI/Game_Over_Screen/Yes button",
		"Art/UI/Game_Over_Screen/No button",
		"Art/UI/Title screen/Title",
		"Art/UI/Title screen/Lantern Title",
		"Art/UI/Title screen/Start button",
		"Art/UI/Title screen/SoundNotice",
	};

	[Test]
	public void EveryUISpriteCoversItsWholeTexture()
	{
		List<string> bad = new List<string>();

		foreach (string path in LoadedByUI)
		{
			Sprite sprite = Resources.Load<Sprite>(path);

			if (sprite == null)
			{
				bad.Add($"{path}: 로드 실패");
				continue;
			}

			Texture2D texture = sprite.texture;

			if (sprite.rect.width < texture.width || sprite.rect.height < texture.height)
				bad.Add($"{path}: 조각 {sprite.rect.width}x{sprite.rect.height}, 원본 {texture.width}x{texture.height}");
		}

		Assert.IsEmpty(bad,
			"Multiple 로 잘린 텍스처는 Resources.Load<Sprite> 가 조각 하나만 돌려주고,"
			+ " UI 는 그 조각을 지정 폭까지 늘려 그린다:\n"
			+ string.Join("\n", bad));
	}

	const string GameOverDir = "Assets/Resources/Art/UI/Game_Over_Screen";
	const string SheetSuffix = "_sheet";

	[Test]
	public void GameOverScreenTexturesImportAsTheirKindDemands()
	{
		List<string> bad = new List<string>();

		foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { GameOverDir }))
		{
			string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
			UnityEditor.TextureImporter importer =
				UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;

			if (importer == null)
				continue;

			bool isSheet = System.IO.Path.GetFileNameWithoutExtension(path).EndsWith(SheetSuffix);
			UnityEditor.SpriteImportMode wanted = isSheet
				? UnityEditor.SpriteImportMode.Multiple
				: UnityEditor.SpriteImportMode.Single;

			if (importer.spriteImportMode != wanted)
				bad.Add($"{path}: spriteImportMode={importer.spriteImportMode}, 기대={wanted}");
		}

		Assert.IsEmpty(bad,
			$"게임오버 화면 이미지는 통짜로 쓰니 Single 이어야 하고, '{SheetSuffix}' 로 끝나는 프레임 시트만"
			+ " Multiple 이다. 통짜가 Multiple 로 들어오면 조각 하나만 로드된다:\n"
			+ string.Join("\n", bad));
	}
}
