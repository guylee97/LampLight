using UnityEngine;

public static class Ease
{
	public static float SmoothStep(float t)
	{
		t = Mathf.Clamp01(t);
		return t * t * (3.0f - 2.0f * t);
	}

	public static float SmootherStep(float t)
	{
		t = Mathf.Clamp01(t);
		return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
	}

	public static float InOutCubic(float t)
	{
		t = Mathf.Clamp01(t);

		if (t < 0.5f)
			return 4.0f * t * t * t;

		float f = -2.0f * t + 2.0f;
		return 1.0f - f * f * f * 0.5f;
	}

	public static float OutQuint(float t)
	{
		t = Mathf.Clamp01(t);
		float f = 1.0f - t;
		return 1.0f - f * f * f * f * f;
	}

	public static float OutBounce(float t)
	{
		t = Mathf.Clamp01(t);

		const float n = 7.5625f;
		const float d = 2.75f;

		if (t < 1.0f / d)
			return n * t * t;

		if (t < 2.0f / d)
		{
			t -= 1.5f / d;
			return n * t * t + 0.75f;
		}

		if (t < 2.5f / d)
		{
			t -= 2.25f / d;
			return n * t * t + 0.9375f;
		}

		t -= 2.625f / d;
		return n * t * t + 0.984375f;
	}
}
