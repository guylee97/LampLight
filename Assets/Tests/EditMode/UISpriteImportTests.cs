using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class UISpriteImportTests
{
	static readonly string[] LoadedByUI =
	{
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
}
