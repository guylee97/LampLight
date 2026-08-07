using UnityEngine;

public class AwarenessMeter : MonoBehaviour
{
	const float Width = 0.9f;
	const float Height = 0.14f;
	const float BorderScale = 1.18f;
	const int SortingOrder = 9000;

	static readonly Color UnawareColor = new Color(0.62f, 0.65f, 0.70f, 0.0f);
	static readonly Color SuspiciousColor = new Color(1.0f, 0.78f, 0.24f, 1.0f);
	static readonly Color AlertedColor = new Color(0.96f, 0.22f, 0.18f, 1.0f);
	static readonly Color BackColor = new Color(0.04f, 0.04f, 0.05f, 0.72f);

	static Sprite s_quad;

	[SerializeField]
	float _heightOffset = 1.35f;

	[SerializeField]
	float _fadeSpeed = 8.0f;

	EnemyBase _owner;
	Transform _pivot;
	SpriteRenderer _back;
	SpriteRenderer _fill;
	float _alpha;

	public static void Attach(EnemyBase owner)
	{
		if (owner == null || owner.GetComponentInChildren<AwarenessMeter>() != null)
			return;

		GameObject go = new GameObject("AwarenessMeter");
		go.transform.SetParent(owner.transform, false);
		go.AddComponent<AwarenessMeter>();
	}

	static Sprite Quad()
	{
		if (s_quad != null)
			return s_quad;

		Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixel(0, 0, Color.white);
		texture.Apply();

		s_quad = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.0f, 0.5f), 1.0f);
		return s_quad;
	}

	void Awake()
	{
		_owner = GetComponentInParent<EnemyBase>();

		GameObject pivot = new GameObject("Pivot");
		pivot.transform.SetParent(transform, false);
		_pivot = pivot.transform;

		_back = CreateBar("Back", BackColor, SortingOrder);
		_fill = CreateBar("Fill", SuspiciousColor, SortingOrder + 1);

		_back.transform.localScale = new Vector3(Width * BorderScale, Height * BorderScale, 1.0f);
		_back.transform.localPosition = new Vector3(-Width * BorderScale * 0.5f, 0.0f, 0.0f);

		ApplyAlpha(0.0f);
	}

	SpriteRenderer CreateBar(string barName, Color color, int order)
	{
		GameObject go = new GameObject(barName);
		go.transform.SetParent(_pivot, false);

		SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
		renderer.sprite = Quad();
		renderer.color = color;
		renderer.sortingOrder = order;
		return renderer;
	}

	void LateUpdate()
	{
		if (_owner == null)
		{
			ApplyAlpha(0.0f);
			return;
		}

		_pivot.position = _owner.transform.position + Vector3.up * _heightOffset;
		_pivot.rotation = Quaternion.identity;

		Define.Awareness awareness = _owner.Awareness;
		float target = awareness == Define.Awareness.Unaware ? 0.0f : 1.0f;
		_alpha = Mathf.MoveTowards(_alpha, target, _fadeSpeed * Time.deltaTime);

		Color color = awareness == Define.Awareness.Alerted ? AlertedColor : SuspiciousColor;
		float ratio = awareness == Define.Awareness.Alerted ? 1.0f : _owner.SearchRatio;

		if (awareness == Define.Awareness.Unaware)
			color = UnawareColor;

		_fill.color = new Color(color.r, color.g, color.b, _alpha);
		_fill.transform.localScale = new Vector3(Width * Mathf.Clamp01(ratio), Height, 1.0f);
		_fill.transform.localPosition = new Vector3(-Width * 0.5f, 0.0f, 0.0f);

		ApplyAlpha(_alpha);
	}

	void ApplyAlpha(float alpha)
	{
		if (_back != null)
			_back.color = new Color(BackColor.r, BackColor.g, BackColor.b, BackColor.a * alpha);

		bool visible = alpha > 0.01f;

		if (_back != null && _back.enabled != visible)
			_back.enabled = visible;

		if (_fill != null && _fill.enabled != visible)
			_fill.enabled = visible;
	}
}
