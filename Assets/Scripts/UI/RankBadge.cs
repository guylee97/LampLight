using System.Collections.Generic;
using UnityEngine;

public static class RankBadge
{
	public const int Size = 128;

	static readonly Dictionary<string, Sprite> s_cache = new Dictionary<string, Sprite>();

	static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
	{
		['S'] = new[] { ".###", "#...", "#...", ".##.", "...#", "...#", "###." },
		['A'] = new[] { ".##.", "#..#", "#..#", "####", "#..#", "#..#", "#..#" },
		['B'] = new[] { "###.", "#..#", "#..#", "###.", "#..#", "#..#", "###." },
		['C'] = new[] { ".###", "#...", "#...", "#...", "#...", "#...", ".###" },
	};

	public static Color TintFor(string grade)
	{
		switch (grade)
		{
			case "S": return new Color(0.95f, 0.82f, 0.45f);
			case "A": return new Color(0.62f, 0.85f, 0.78f);
			case "B": return new Color(0.68f, 0.72f, 0.78f);
			default: return new Color(0.55f, 0.55f, 0.58f);
		}
	}

	public static Sprite Get(string grade)
	{
		if (string.IsNullOrEmpty(grade))
			grade = "C";

		Sprite cached;
		if (s_cache.TryGetValue(grade, out cached) && cached != null)
			return cached;

		Sprite made = Build(grade);
		s_cache[grade] = made;
		return made;
	}

	static Sprite Build(string grade)
	{
		Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
		texture.filterMode = FilterMode.Point;

		Color tint = TintFor(grade);
		Color[] pixels = new Color[Size * Size];

		float center = (Size - 1) * 0.5f;
		float outer = Size * 0.46f;
		float inner = Size * 0.38f;

		for (int y = 0; y < Size; y++)
		{
			for (int x = 0; x < Size; x++)
			{
				float d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
				Color c = new Color(0, 0, 0, 0);

				if (d <= inner)
					c = new Color(0.05f, 0.07f, 0.09f, 0.92f);
				else if (d <= outer)
					c = tint;

				pixels[y * Size + x] = c;
			}
		}

		Stamp(pixels, grade[0], tint);

		texture.SetPixels(pixels);
		texture.Apply();

		return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
	}

	static void Stamp(Color[] pixels, char letter, Color tint)
	{
		string[] glyph;
		if (Glyphs.TryGetValue(letter, out glyph) == false)
			return;

		int rows = glyph.Length;
		int cols = glyph[0].Length;
		int scale = Size / 14;

		int originX = (Size - cols * scale) / 2;
		int originY = (Size - rows * scale) / 2;

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				if (glyph[rows - 1 - r][c] != '#')
					continue;

				for (int dy = 0; dy < scale; dy++)
				{
					for (int dx = 0; dx < scale; dx++)
					{
						int x = originX + c * scale + dx;
						int y = originY + r * scale + dy;

						if (x < 0 || x >= Size || y < 0 || y >= Size)
							continue;

						pixels[y * Size + x] = tint;
					}
				}
			}
		}
	}
}
