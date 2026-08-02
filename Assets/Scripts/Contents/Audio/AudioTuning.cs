using UnityEngine;

public static class AudioTuning
{
	public const float PingCoefficient = 0.30f;
	public const float PingMinPeriod = 0.25f;

	public const float OcclusionFactor = 0.45f;
	public const float OcclusionRefreshSeconds = 0.1f;

	public const float DuckGainDb = -7.0f;
	public const float DuckAttackSeconds = 0.030f;
	public const float DuckReleaseSeconds = 0.250f;

	public const float ScheduleLookaheadSeconds = 0.05f;

	public static float HearingScale = 1.0f;

	public static void ResetRuntimeState()
	{
		HearingScale = 1.0f;
	}

	public static float ArtifactRadius(int level)
	{
		switch (level)
		{
			case 1: return 12.0f;
			case 2: return 9.0f;
			default: return 7.0f;
		}
	}

	public static bool IsReady(AudioClip clip)
	{
		if (clip == null)
			return false;

		if (clip.loadState == AudioDataLoadState.Loaded)
			return true;

		if (clip.loadState == AudioDataLoadState.Unloaded)
			clip.LoadAudioData();

		return false;
	}

	public static float BusDb(Define.Sound bus)
	{
		switch (bus)
		{
			case Define.Sound.Guide: return -6.0f;
			case Define.Sound.Threat: return 0.0f;
			case Define.Sound.Self: return -3.0f;
			case Define.Sound.Ambient: return -30.0f;
			case Define.Sound.UI: return -9.0f;
			default: return 0.0f;
		}
	}

	public static float DbToLinear(float db)
	{
		return Mathf.Pow(10.0f, db / 20.0f);
	}

	public static float BusGain(Define.Sound bus)
	{
		return DbToLinear(BusDb(bus));
	}

	public static float PingPeriod(float distanceTiles)
	{
		return Mathf.Max(PingMinPeriod, PingCoefficient * distanceTiles);
	}

	public static float ClearWeight(int wallCount)
	{
		if (wallCount <= 0)
			return 1.0f;

		return Mathf.Pow(OcclusionFactor, wallCount);
	}

	public static AnimationCurve BuildRolloffCurve()
	{
		AnimationCurve curve = new AnimationCurve();
		curve.AddKey(0.000f, 1.000f);
		curve.AddKey(0.167f, 1.000f);
		curve.AddKey(0.500f, DbToLinear(-4.0f));
		curve.AddKey(0.833f, DbToLinear(-9.0f));
		curve.AddKey(0.950f, DbToLinear(-14.0f));
		curve.AddKey(1.000f, 0.000f);

		for (int i = 0; i < curve.length; i++)
			curve.SmoothTangents(i, 0.0f);

		return curve;
	}

	public static void ApplySpatial(AudioSource source, float radiusTiles)
	{
		source.spatialBlend = 1.0f;
		source.dopplerLevel = 0.0f;
		source.rolloffMode = AudioRolloffMode.Custom;
		source.minDistance = 0.0f;
		source.maxDistance = Mathf.Max(0.01f, radiusTiles);
		source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, BuildRolloffCurve());
	}
}
