using UnityEngine;

public class DirectionalSprite : MonoBehaviour
{
	[SerializeField]
	SpriteRenderer _renderer;

	[SerializeField]
	Sprite[] _sprites = new Sprite[DirectionUtil.Count];

	[SerializeField]
	Define.Direction8 _direction = Define.Direction8.S;

	bool _applied;

	public Define.Direction8 Direction { get { return _direction; } }

	public void Apply(Define.Direction8 direction)
	{
		if (_applied && _direction == direction)
			return;

		_direction = direction;
		_applied = true;

		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		if (_renderer == null)
			return;

		int index = (int)direction;
		if (_sprites == null || index < 0 || index >= _sprites.Length)
			return;

		Sprite sprite = _sprites[index];
		if (sprite != null)
			_renderer.sprite = sprite;
	}
}
