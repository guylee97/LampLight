using UnityEngine;

public class MapCollisionBaker : MonoBehaviour
{
	public const string RootName = "@BakedCollision";

	GameObject _root;

	public int Count { get; private set; }

	public void Build(MapData map)
	{
		Clear();

		if (map == null || map.collision == null
			|| map.collision.Length != map.width * map.height)
			return;

		_root = new GameObject(RootName);
		_root.transform.SetParent(transform, false);
		_root.layer = (int)Define.Layer.Block;

		Rigidbody2D body = _root.AddComponent<Rigidbody2D>();
		body.bodyType = RigidbodyType2D.Static;

		bool[] taken = new bool[map.width * map.height];
		int made = 0;

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (taken[row * map.width + col] || NeedsCollider(map, col, row) == false)
					continue;

				int width = 1;
				while (col + width < map.width
					&& taken[row * map.width + col + width] == false
					&& NeedsCollider(map, col + width, row))
					width++;

				int height = 1;
				while (row + height < map.height && SpanIsFree(map, taken, col, row + height, width))
					height++;

				for (int r = row; r < row + height; r++)
				{
					for (int c = col; c < col + width; c++)
						taken[r * map.width + c] = true;
				}

				BoxCollider2D box = _root.AddComponent<BoxCollider2D>();
				box.size = new Vector2(width, height);
				box.offset = new Vector2(
					col + width * 0.5f,
					map.height - (row + height) + height * 0.5f);
				made++;
			}
		}

		Count = made;
	}

	static bool NeedsCollider(MapData map, int col, int row)
	{
		return map.collision[row * map.width + col] == MapCoord.CollisionBlock
			&& map.GetGid(map.walls, col, row) == 0;
	}

	static bool SpanIsFree(MapData map, bool[] taken, int col, int row, int width)
	{
		for (int c = col; c < col + width; c++)
		{
			if (taken[row * map.width + c] || NeedsCollider(map, c, row) == false)
				return false;
		}

		return true;
	}

	public void Clear()
	{
		if (_root == null)
			return;

		if (Application.isPlaying)
			Destroy(_root);
		else
			DestroyImmediate(_root);

		_root = null;
		Count = 0;
	}
}
