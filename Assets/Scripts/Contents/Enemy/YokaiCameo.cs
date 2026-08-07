using System.Collections;
using UnityEngine;

public class YokaiCameo : MonoBehaviour
{
	const float LeadTiles = 4.5f;
	const float CrossTiles = 3.0f;
	const float CrossSeconds = 1.35f;
	const float FadeSeconds = 0.30f;
	const float PeakAlpha = 0.72f;
	const float Dimming = 0.38f;
	const int SortingOrder = 40;

	static Material _unlit;
	static bool _playing;

	public static bool IsPlaying { get { return _playing; } }

	public static bool Play(YokaiSpec spec, Transform viewer)
	{
		if (_playing || spec == null || viewer == null || MapCoord.IsReady == false)
			return false;

		Vector2 across;
		Vector3 origin;
		if (FindCrossing(viewer.position, ViewerHeading(viewer), out origin, out across) == false)
			return false;

		GameObject go = new GameObject("YokaiCameo");
		go.transform.position = origin;
		go.AddComponent<YokaiCameo>().Begin(spec, across);
		return true;
	}

	static Vector2 ViewerHeading(Transform viewer)
	{
		PlayerController player = viewer.GetComponent<PlayerController>();
		Vector2 heading = player != null ? player.FacingDirection : Vector2.down;
		return heading.sqrMagnitude <= 0.001f ? Vector2.down : heading.normalized;
	}

	static bool FindCrossing(Vector3 from, Vector2 heading, out Vector3 origin, out Vector2 across)
	{
		for (int turn = 0; turn < 4; turn++)
		{
			Vector2 forward = Rotate(heading, turn * 90.0f);
			Vector2 side = new Vector2(-forward.y, forward.x);

			Vector3 centre = from + (Vector3)(forward * LeadTiles);
			Vector3 start = centre - (Vector3)(side * CrossTiles * 0.5f);
			Vector3 end = centre + (Vector3)(side * CrossTiles * 0.5f);

			if (Walkable(start) && Walkable(centre) && Walkable(end))
			{
				origin = start;
				across = side;
				return true;
			}
		}

		origin = Vector3.zero;
		across = Vector2.zero;
		return false;
	}

	static Vector2 Rotate(Vector2 v, float degrees)
	{
		float rad = degrees * Mathf.Deg2Rad;
		float c = Mathf.Cos(rad);
		float s = Mathf.Sin(rad);
		return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
	}

	static bool Walkable(Vector3 world)
	{
		Vector2Int tile = MapCoord.WorldToTile(world);
		return MapCoord.IsPassable(tile.x, tile.y);
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

	SpriteRenderer _renderer;
	Color _base;

	void Begin(YokaiSpec spec, Vector2 across)
	{
		GameObject art = new GameObject("Art");
		art.transform.SetParent(transform, false);

		_renderer = art.AddComponent<SpriteRenderer>();
		_renderer.material = Unlit();
		_renderer.sortingOrder = SortingOrder;

		_base = spec.Tint * Dimming;
		_base.a = 0.0f;
		_renderer.color = _base;

		DirectionalSpriteAnimator animator = gameObject.AddComponent<DirectionalSpriteAnimator>();
		animator.UseRenderer(_renderer);
		animator.SetCharacter(spec.CharacterKey);
		animator.SetHeading(across);
		animator.SetState(DirectionalSpriteAnimator.StateWalk);

		StartCoroutine(Cross(across));
	}

	IEnumerator Cross(Vector2 across)
	{
		_playing = true;

		Vector3 start = transform.position;
		Vector3 end = start + (Vector3)(across * CrossTiles);

		for (float t = 0.0f; t < CrossSeconds; t += Time.deltaTime)
		{
			float k = t / CrossSeconds;
			transform.position = Vector3.Lerp(start, end, k);

			float fade = Mathf.Min(
				Mathf.Clamp01(t / FadeSeconds),
				Mathf.Clamp01((CrossSeconds - t) / FadeSeconds));

			Color color = _base;
			color.a = PeakAlpha * fade;
			_renderer.color = color;

			yield return null;
		}

		_playing = false;
		Destroy(gameObject);
	}

	void OnDestroy()
	{
		_playing = false;
	}
}
