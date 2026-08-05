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

		int made = 0;

		for (int row = 0; row < map.height; row++)
		{
			for (int col = 0; col < map.width; col++)
			{
				if (map.collision[row * map.width + col] != MapCoord.CollisionBlock)
					continue;

				if (map.GetGid(map.walls, col, row) != 0)
					continue;

				BoxCollider2D box = _root.AddComponent<BoxCollider2D>();
				box.size = Vector2.one;
				box.offset = new Vector2(col + 0.5f, map.height - 1 - row + 0.5f);
				made++;
			}
		}

		Count = made;
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
