using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterState
{
	public string name;
	public string resource;
	public int cols;
	public int rows;
	public int frames;
	public string directionAxis;
	public float fps;
	public float fpsSneak;
	public float fpsWalk;
	public float fpsRun;

	public bool DirectionIsRow { get { return directionAxis == "row"; } }
}

[Serializable]
public class CharacterSpec
{
	public string key;
	public int frameWidth;
	public int frameHeight;
	public int footBaselineY;
	public float pivotX;
	public float pivotY;
	public float colliderW;
	public float colliderH;
	public float speedMultiplier;
	public float noiseDetectRadiusTiles;
	public float searchDurationSec;
	public string[] directions;
	public CharacterState[] states;

	public CharacterState State(string stateName)
	{
		if (states == null)
			return null;

		foreach (CharacterState state in states)
		{
			if (state.name == stateName)
				return state;
		}

		return null;
	}

	public int DirectionIndex(string direction)
	{
		if (directions == null)
			return 0;

		for (int i = 0; i < directions.Length; i++)
		{
			if (directions[i] == direction)
				return i;
		}

		return 0;
	}

	public Vector2 ColliderSize(int pixelsPerUnit)
	{
		return new Vector2(colliderW / pixelsPerUnit, colliderH / pixelsPerUnit);
	}
}

[Serializable]
public class CharacterCatalogData
{
	public CharacterSpec[] characters;
}

public static class CharacterCatalog
{
	public const string CatalogResource = "Data/character_catalog";
	public const int DirectionCount = 8;

	static readonly string[] Compass = { "s", "se", "e", "ne", "n", "nw", "w", "sw" };

	static CharacterCatalogData _data;
	static Dictionary<string, CharacterSpec> _byKey;

	public static bool IsReady { get { return Data != null; } }

	public static CharacterCatalogData Data
	{
		get
		{
			if (_data == null)
				Load();

			return _data;
		}
	}

	public static void Invalidate()
	{
		_data = null;
		_byKey = null;
	}

	public static void Load()
	{
		TextAsset text = Resources.Load<TextAsset>(CatalogResource);
		if (text == null)
		{
			Debug.LogError($"CharacterCatalog: Resources/{CatalogResource}.json 없음. "
				+ "Tools/character_manifest_to_unity.py 를 돌려라");
			return;
		}

		_data = JsonUtility.FromJson<CharacterCatalogData>(text.text);
		_byKey = null;

		if (_data == null || _data.characters == null || _data.characters.Length == 0)
			Debug.LogError("CharacterCatalog: character_catalog.json 파싱 실패");
	}

	public static CharacterSpec Get(string key)
	{
		if (Data == null)
			return null;

		if (_byKey == null)
		{
			_byKey = new Dictionary<string, CharacterSpec>();
			foreach (CharacterSpec spec in _data.characters)
				_byKey[spec.key] = spec;
		}

		CharacterSpec found;
		return _byKey.TryGetValue(key, out found) ? found : null;
	}

	public static string DirectionFrom(Vector2 heading)
	{
		if (heading.sqrMagnitude < 0.0001f)
			return Compass[0];

		float angle = Mathf.Atan2(heading.x, -heading.y) * Mathf.Rad2Deg;
		if (angle < 0.0f)
			angle += 360.0f;

		int index = Mathf.RoundToInt(angle / 45.0f) % DirectionCount;
		return Compass[index];
	}

	public static string SpriteName(string characterKey, string state, string direction, int frame)
	{
		return $"{characterKey}_{state}_{direction}_{frame:D2}";
	}
}
