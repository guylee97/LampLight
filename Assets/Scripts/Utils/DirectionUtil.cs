using UnityEngine;

public static class DirectionUtil
{
	public const int Count = 8;

	static readonly Vector2[] _vectors = BuildVectors();

	static Vector2[] BuildVectors()
	{
		Vector2[] vectors = new Vector2[Count];
		for (int i = 0; i < Count; i++)
		{
			float radian = i * 45.0f * Mathf.Deg2Rad;
			vectors[i] = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
		}

		return vectors;
	}

	public static Define.Direction8 FromVector(Vector2 direction, Define.Direction8 fallback)
	{
		if (direction.sqrMagnitude <= Mathf.Epsilon)
			return fallback;

		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		int index = Mathf.RoundToInt(angle / 45.0f);

		index %= Count;
		if (index < 0)
			index += Count;

		return (Define.Direction8)index;
	}

	public static Vector2 ToVector(Define.Direction8 direction)
	{
		int index = (int)direction;
		return index >= 0 && index < Count ? _vectors[index] : Vector2.down;
	}
}
