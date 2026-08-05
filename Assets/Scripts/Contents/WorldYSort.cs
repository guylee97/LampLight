using UnityEngine;

public class WorldYSort : MonoBehaviour
{
	public const int BaseOrder = 1000;
	public const int UnitsPerOrder = 10;

	SpriteRenderer[] _renderers;

	void Awake()
	{
		_renderers = GetComponentsInChildren<SpriteRenderer>(true);
		Apply();
	}

	void LateUpdate()
	{
		Apply();
	}

	void Apply()
	{
		int order = OrderFor(transform.position.y);
		foreach (SpriteRenderer renderer in _renderers)
		{
			if (renderer != null)
				renderer.sortingOrder = order;
		}
	}

	public static int OrderFor(float worldY)
	{
		return BaseOrder - Mathf.RoundToInt(worldY * UnitsPerOrder);
	}
}
