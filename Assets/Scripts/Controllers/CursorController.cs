using UnityEngine;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
	int _mask = (1 << (int)Define.Layer.Ground) | (1 << (int)Define.Layer.Enemy);

	Texture2D _attackIcon;
	Texture2D _handIcon;

	enum CursorType
	{
		None,
		Attack,
		Hand,
	}

	CursorType _cursorType = CursorType.None;

	void Start()
	{
		_attackIcon = Managers.Resource.Load<Texture2D>("Textures/Cursor/Attack");
		_handIcon = Managers.Resource.Load<Texture2D>("Textures/Cursor/Hand");
	}

	void Update()
	{
		Mouse mouse = Mouse.current;
		if (mouse == null || mouse.leftButton.isPressed || Camera.main == null)
			return;

		Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
		Vector2 mousePosition = mouseWorldPosition;

		Collider2D hit = Physics2D.OverlapPoint(mousePosition, _mask);
		if (hit == null)
			return;

		if (hit.gameObject.layer == (int)Define.Layer.Enemy)
		{
			SetCursor(CursorType.Attack, _attackIcon, 0.2f);
		}
		else
		{
			SetCursor(CursorType.Hand, _handIcon, 0.33f);
		}
	}

	void SetCursor(CursorType cursorType, Texture2D texture, float hotspotRatio)
	{
		if (_cursorType == cursorType || texture == null)
			return;

		Vector2 hotspot = new Vector2(texture.width * hotspotRatio, 0);
		Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
		_cursorType = cursorType;
	}
}
