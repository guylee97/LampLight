using UnityEngine;

public class FallingDust : MonoBehaviour
{
	const float FallSeconds = 1.15f;
	const float FallTiles = 1.6f;
	const float DriftTiles = 0.22f;
	const float MoteTiles = 0.07f;
	const int SortingOrder = 30;

	static Sprite _pixel;

	public static void Burst(Vector3 centre, int motes, float spreadTiles, int seed)
	{
		if (motes <= 0)
			return;

		System.Random rng = new System.Random(seed);

		for (int i = 0; i < motes; i++)
		{
			float dx = ((float)rng.NextDouble() * 2.0f - 1.0f) * spreadTiles;
			float dy = (float)rng.NextDouble() * spreadTiles * 0.5f;

			GameObject go = new GameObject("Dust");
			go.transform.position = centre + new Vector3(dx, dy, 0.0f);

			SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
			renderer.sprite = Pixel();
			renderer.sortingOrder = SortingOrder;
			renderer.color = new Color(0.62f, 0.58f, 0.50f, 0.0f);
			go.transform.localScale = Vector3.one * MoteTiles;

			FallingDust mote = go.AddComponent<FallingDust>();
			mote._drift = ((float)rng.NextDouble() * 2.0f - 1.0f) * DriftTiles;
			mote._span = FallSeconds * (0.7f + (float)rng.NextDouble() * 0.6f);
			mote._renderer = renderer;
			mote._origin = go.transform.position;
		}
	}

	static Sprite Pixel()
	{
		if (_pixel == null)
		{
			Texture2D texture = new Texture2D(1, 1);
			texture.filterMode = FilterMode.Point;
			texture.SetPixel(0, 0, Color.white);
			texture.Apply();
			_pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1.0f);
		}

		return _pixel;
	}

	SpriteRenderer _renderer;
	Vector3 _origin;
	float _drift;
	float _span;
	float _elapsed;

	void Update()
	{
		_elapsed += Time.deltaTime;

		float k = Mathf.Clamp01(_elapsed / _span);

		transform.position = _origin + new Vector3(
			_drift * k,
			-FallTiles * k * k,
			0.0f);

		Color color = _renderer.color;
		color.a = Mathf.Sin(k * Mathf.PI) * 0.85f;
		_renderer.color = color;

		if (k >= 1.0f)
			Destroy(gameObject);
	}
}
