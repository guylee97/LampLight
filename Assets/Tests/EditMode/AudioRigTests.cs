using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class AudioRigTests
{
	const string SoundDir = "Assets/Resources/Sounds";

	[Test]
	public void EveryClipPreloads()
	{
		string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SoundDir });
		Assert.Greater(guids.Length, 0, "Resources/Sounds에 클립이 없다");

		List<string> bad = new List<string>();

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;

			if (importer == null)
				continue;

			if (importer.defaultSampleSettings.preloadAudioData == false)
				bad.Add(System.IO.Path.GetFileName(path));
		}

		Assert.IsEmpty(bad,
			"preload이 꺼진 클립은 loadState가 Unloaded로 남아 재생 가드에 막힌다:\n"
			+ string.Join(", ", bad));
	}

	[Test]
	public void RolloffReachesFullVolumeAtZeroDistance()
	{
		AnimationCurve curve = AudioTuning.BuildRolloffCurve();
		Assert.AreEqual(1.0f, curve.Evaluate(0.0f), 0.001f,
			"리스너와 음원이 같은 지점이면 감쇠가 없어야 한다");
	}

	[Test]
	public void ListenerOffsetWouldSilenceTheGuideBus()
	{
		AnimationCurve curve = AudioTuning.BuildRolloffCurve();

		float cameraZ = 10.0f;
		float worstRadius = AudioTuning.ArtifactRadius(3);

		float normalized = cameraZ / worstRadius;
		Assert.Greater(normalized, 1.0f,
			"이 테스트의 전제가 깨졌다 — 카메라 Z오프셋 계산을 다시 확인할 것");

		Assert.AreEqual(0.0f, curve.Evaluate(1.0f), 0.001f,
			"반경 밖은 무음이다. 따라서 리스너는 반드시 플레이어에 붙어 있어야 한다");
	}
}
