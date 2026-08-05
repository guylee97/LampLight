using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundRingGraphic : MaskableGraphic
{
	public const float Duration = 0.6f;

	public struct Entry
	{
		public float Angle;
		public float HalfWidth;
		public float Expiry;
		public Color Tint;
	}

	readonly List<Entry> _entries = new List<Entry>();

	[SerializeField]
	float _thickness = 6.0f;

	[SerializeField]
	float _radiusRatio = 0.45f;

	[SerializeField]
	int _segments = 14;

	public int ActiveCount { get { return _entries.Count; } }

	public void Emit(float angle, float halfWidth, Color tint)
	{
		Entry entry;
		entry.Angle = angle;
		entry.HalfWidth = Mathf.Clamp(halfWidth, 0.05f, 1.2f);
		entry.Expiry = Time.unscaledTime + Duration;
		entry.Tint = tint;

		_entries.Add(entry);
		SetVerticesDirty();
	}

	void Update()
	{
		if (_entries.Count == 0)
			return;

		float now = Time.unscaledTime;
		bool changed = false;

		for (int i = _entries.Count - 1; i >= 0; i--)
		{
			if (_entries[i].Expiry > now)
				continue;

			_entries.RemoveAt(i);
			changed = true;
		}

		SetVerticesDirty();

		if (changed == false)
			return;
	}

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();

		if (_entries.Count == 0)
			return;

		Rect rect = GetPixelAdjustedRect();
		float radius = Mathf.Min(rect.width, rect.height) * _radiusRatio;
		float now = Time.unscaledTime;

		foreach (Entry entry in _entries)
		{
			float life = Mathf.Clamp01((entry.Expiry - now) / Duration);
			if (life <= 0.0f)
				continue;

			Color color = entry.Tint;
			color.a *= life;

			AddArc(vh, radius, entry.Angle, entry.HalfWidth, color);
		}
	}

	void AddArc(VertexHelper vh, float radius, float angle, float halfWidth, Color color)
	{
		int steps = Mathf.Max(2, _segments);
		float inner = radius - _thickness * 0.5f;
		float outer = radius + _thickness * 0.5f;

		for (int i = 0; i <= steps; i++)
		{
			float t = i / (float)steps;
			float a = angle - halfWidth + halfWidth * 2.0f * t;
			float taper = Mathf.Sin(t * Mathf.PI);

			Color c = color;
			c.a *= taper;

			Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

			UIVertex v = UIVertex.simpleVert;
			v.color = c;

			v.position = dir * inner;
			vh.AddVert(v);

			v.position = dir * outer;
			vh.AddVert(v);
		}

		for (int i = 0; i < steps; i++)
		{
			int b = i * 2;
			vh.AddTriangle(b, b + 1, b + 3);
			vh.AddTriangle(b, b + 3, b + 2);
		}
	}
}

public class SoundRing : MonoBehaviour
{
	public static readonly Color GuideColor = new Color(0.90f, 0.76f, 0.53f, 0.85f);
	public static readonly Color ThreatColor = new Color(0.95f, 0.38f, 0.38f, 0.85f);
	public static readonly Color SelfColor = new Color(0.60f, 0.65f, 0.70f, 0.65f);

	static SoundRing s_instance;

	[SerializeField]
	bool _enabledByDefault = true;

	SoundRingGraphic _graphic;
	bool _visible;

	public static bool IsVisible { get { return s_instance != null && s_instance._visible; } }

	public static void SetVisible(bool visible)
	{
		if (s_instance != null)
			s_instance._visible = visible;
	}

	public static void Emit(Vector2 direction, Color tint, float distanceTiles = 0.0f)
	{
		if (s_instance == null || s_instance._visible == false)
			return;

		if (direction.sqrMagnitude <= 0.0001f)
			return;

		float angle = Mathf.Atan2(direction.y, direction.x);
		float halfWidth = Mathf.Lerp(0.18f, 0.55f, Mathf.Clamp01(distanceTiles / 12.0f));

		s_instance.EmitInternal(angle, halfWidth, tint);
	}

	public static void EmitOmni(Color tint)
	{
		if (s_instance == null || s_instance._visible == false)
			return;

		float quarter = Mathf.PI * 0.5f;
		for (int i = 0; i < 4; i++)
			s_instance.EmitInternal(quarter * i, quarter * 0.5f, tint);
	}

	void Awake()
	{
		s_instance = this;
		_visible = _enabledByDefault;
		// 소리 방향 HUD 기능 제거.
		// BuildCanvas();
	}

	void OnDestroy()
	{
		if (s_instance == this)
			s_instance = null;
	}

	void EmitInternal(float angle, float halfWidth, Color tint)
	{
		// 소리 방향 HUD 기능 제거.
		// if (_graphic == null)
		// 	BuildCanvas();
		//
		// if (_graphic != null)
		// 	_graphic.Emit(angle, halfWidth, tint);
	}

	void BuildCanvas()
	{
		if (_graphic != null)
			return;

		GameObject canvasGo = new GameObject("SoundRingCanvas");
		canvasGo.transform.SetParent(transform, false);

		Canvas canvas = canvasGo.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 500;

		CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

		GameObject graphicGo = new GameObject("Ring");
		graphicGo.transform.SetParent(canvasGo.transform, false);

		RectTransform rect = graphicGo.AddComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		_graphic = graphicGo.AddComponent<SoundRingGraphic>();
		_graphic.raycastTarget = false;
	}
}
