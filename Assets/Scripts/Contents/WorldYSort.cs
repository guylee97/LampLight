using UnityEngine;

public class WorldYSort : MonoBehaviour
{
	public const int BaseOrder = 1000;
	public const int UnitsPerOrder = 10;

	SpriteRenderer[] _renderers;
	Collider2D _body;

	void Awake()
	{
		_renderers = GetComponentsInChildren<SpriteRenderer>(true);
		_body = GetComponent<Collider2D>();
		Apply();
	}

	void LateUpdate()
	{
		Apply();
	}

	void Apply()
	{
		int order = OrderFor(BaseY());
		foreach (SpriteRenderer renderer in _renderers)
		{
			if (renderer != null)
				renderer.sortingOrder = order;
		}
	}

	float BaseY()
	{
		return _body != null ? _body.bounds.min.y : transform.position.y;
	}

	public static int OrderFor(float worldY)
	{
		return BaseOrder - Mathf.RoundToInt(worldY * UnitsPerOrder);
	}
}
