using UnityEngine;
using UnityEngine.UI;

public static class KoreanFont
{
	public const string ResourcePath = "Fonts/Silver";

	static Font _font;
	static bool _looked;

	public static Font Font
	{
		get
		{
			if (_looked)
				return _font;

			_looked = true;
			_font = Resources.Load<Font>(ResourcePath);

			if (_font == null)
				Debug.LogError($"KoreanFont: Resources/{ResourcePath} 없음");

			return _font;
		}
	}

	public static void Apply(Text text)
	{
		if (text == null || Font == null)
			return;

		text.font = Font;
	}

	public static void ApplyAll(GameObject root)
	{
		if (root == null || Font == null)
			return;

		foreach (Text text in root.GetComponentsInChildren<Text>(true))
			text.font = Font;
	}
}
