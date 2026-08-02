using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SetPieceObject
{
	public string key;
	public float tileX;
	public float tileY;
}

[Serializable]
public class SetPiece
{
	public string name;
	public string tag;
	public int cols;
	public int rows;
	public SetPieceObject[] objects;

	public bool FitsIn(MapRoom room)
	{
		return room.width >= cols && room.height >= rows;
	}
}

[Serializable]
public class SetPieceCatalogData
{
	public SetPiece[] pieces;
}

public static class SetPieceCatalog
{
	public const string CatalogResource = "Data/setpiece_catalog";

	public const string TagAltar = "altar";
	public const string TagExit = "exit";
	public const string TagGeneric = "generic";

	static SetPieceCatalogData _data;

	public static bool IsReady { get { return Data != null && Data.pieces != null && Data.pieces.Length > 0; } }

	public static SetPieceCatalogData Data
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
	}

	public static void Load()
	{
		TextAsset text = Resources.Load<TextAsset>(CatalogResource);
		if (text == null)
		{
			Debug.LogWarning($"SetPieceCatalog: Resources/{CatalogResource}.json 없음. "
				+ "Tools/setpiece_to_unity.py 를 돌려라");
			return;
		}

		_data = JsonUtility.FromJson<SetPieceCatalogData>(text.text);
	}

	public static List<SetPiece> ByTag(string tag, MapRoom room)
	{
		List<SetPiece> matched = new List<SetPiece>();
		if (IsReady == false)
			return matched;

		foreach (SetPiece piece in _data.pieces)
		{
			if (piece.tag == tag && piece.FitsIn(room))
				matched.Add(piece);
		}

		return matched;
	}
}
