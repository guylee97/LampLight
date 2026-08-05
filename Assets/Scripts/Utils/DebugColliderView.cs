using System.Collections.Generic;
using UnityEngine;

public class DebugColliderView : MonoBehaviour
{
	public const int SortingOrder = 32000;

	public static readonly Color ActorColor = new Color(1.0f, 0.15f, 0.15f, 1.0f);
	public static readonly Color BlockColor = new Color(1.0f, 0.35f, 0.1f, 0.9f);
	public static readonly Color SpriteColor = new Color(0.2f, 0.9f, 1.0f, 0.85f);

	const float Thickness = 0.04f;

	static Sprite _pixel;
	static Material _unlit;

	readonly List<GameObject> _lines = new List<GameObject>();

	public void Build()
	{
		Clear();

		foreach (Collider2D collider in FindObjectsByType<Collider2D>(
			FindObjectsInactive.Exclude, FindObjectsSortMode.None))
		{
			if (collider.isTrigger)
				continue;

			bool isActor = collider.GetComponentInParent<PlayerController>() != null
				|| collider.GetComponentInParent<EnemyBase>() != null;

			CompositeCollider2D composite = collider as CompositeCollider2D;
			if (composite != null)
			{
				DrawComposite(composite);
				continue;
			}

			if (collider.usedByComposite)
				continue;

			DrawShape(collider, isActor ? ActorColor : BlockColor);

			if (isActor)
				DrawActorSprite(collider);
		}
	}

	void DrawShape(Collider2D collider, Color color)
	{
		Vector2 size;
		Vector2 offset;

		CapsuleCollider2D capsule = collider as CapsuleCollider2D;
		BoxCollider2D box = collider as BoxCollider2D;
		CircleCollider2D circle = collider as CircleCollider2D;

		if (capsule != null)
		{
			size = capsule.size;
			offset = capsule.offset;
		}
		else if (box != null)
		{
			size = box.size;
			offset = box.offset;
		}
		else if (circle != null)
		{
			size = Vector2.one * circle.radius * 2.0f;
			offset = circle.offset;
		}
		else
		{
			DrawBounds(collider.bounds, color);
			return;
		}

		Transform t = collider.transform;
		Vector2 center = t.TransformPoint(offset);
		Vector2 half = new Vector2(
			size.x * 0.5f * Mathf.Abs(t.lossyScale.x),
			size.y * 0.5f * Mathf.Abs(t.lossyScale.y));

		DrawBounds(new Bounds(center, half * 2.0f), color);
	}

	void DrawActorSprite(Collider2D collider)
	{
		SpriteRenderer renderer = collider.GetComponentInChildren<SpriteRenderer>();
		if (renderer != null && renderer.sprite != null)
			DrawBounds(renderer.bounds, SpriteColor);
	}

	void DrawComposite(CompositeCollider2D composite)
	{
		List<Vector2> points = new List<Vector2>();

		for (int i = 0; i < composite.pathCount; i++)
		{
			points.Clear();
			composite.GetPath(i, points);

			for (int p = 0; p < points.Count; p++)
			{
				Vector2 a = composite.transform.TransformPoint(points[p]);
				Vector2 b = composite.transform.TransformPoint(points[(p + 1) % points.Count]);
				DrawSegment(a, b, BlockColor);
			}
		}
	}

	void DrawBounds(Bounds bounds, Color color)
	{
		Vector2 min = bounds.min;
		Vector2 max = bounds.max;

		DrawSegment(new Vector2(min.x, min.y), new Vector2(max.x, min.y), color);
		DrawSegment(new Vector2(max.x, min.y), new Vector2(max.x, max.y), color);
		DrawSegment(new Vector2(max.x, max.y), new Vector2(min.x, max.y), color);
		DrawSegment(new Vector2(min.x, max.y), new Vector2(min.x, min.y), color);
	}

	void DrawSegment(Vector2 a, Vector2 b, Color color)
	{
		Vector2 delta = b - a;
		float length = delta.magnitude;
		if (length <= 0.0001f)
			return;

		GameObject go = new GameObject("line");
		go.transform.SetParent(transform, false);
		go.transform.position = (a + b) * 0.5f;
		go.transform.right = delta / length;
		go.transform.localScale = new Vector3(length, Thickness, 1.0f);

		SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
		renderer.sprite = Pixel();
		renderer.color = color;
		renderer.sharedMaterial = Unlit();
		renderer.sortingOrder = SortingOrder;

		_lines.Add(go);
	}

	public void Clear()
	{
		foreach (GameObject line in _lines)
		{
			if (line == null)
				continue;

			if (Application.isPlaying)
				Destroy(line);
			else
				DestroyImmediate(line);
		}

		_lines.Clear();
	}

	static Sprite Pixel()
	{
		if (_pixel == null)
		{
			Texture2D texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, Color.white);
			texture.Apply();
			_pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1.0f);
		}

		return _pixel;
	}

	static Material Unlit()
	{
		if (_unlit == null)
		{
			Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
			if (shader == null)
				shader = Shader.Find("Sprites/Default");

			_unlit = new Material(shader);
		}

		return _unlit;
	}
}
