using NUnit.Framework;
using UnityEngine;

public class StageFlowTests
{
	GameObject _host;
	StageProgress _progress;

	[SetUp]
	public void SetUp()
	{
		_host = new GameObject("StageProgressHost");
		_progress = _host.AddComponent<StageProgress>();
		Managers.Game.Clear();
	}

	[TearDown]
	public void TearDown()
	{
		Object.DestroyImmediate(_host);
		Managers.Game.Clear();
	}

	[Test]
	public void CollectingRaisesProgressAndCompletes()
	{
		int lastCollected = 0;
		int completedCount = 0;

		_progress.OnArtifactCollected += (collected, required) => lastCollected = collected;
		_progress.OnAllArtifactsCollected += () => completedCount++;

		for (int i = 0; i < _progress.Required; i++)
			_progress.ReportCollected();

		Assert.AreEqual(_progress.Required, lastCollected);
		Assert.IsTrue(_progress.IsComplete);
		Assert.AreEqual(1, completedCount);
	}

	[Test]
	public void ExtraCollectsDoNotOvershoot()
	{
		for (int i = 0; i < _progress.Required + 5; i++)
			_progress.ReportCollected();

		Assert.AreEqual(_progress.Required, _progress.Collected);
	}

	[Test]
	public void ResetProgressClearsCount()
	{
		_progress.ReportCollected();
		_progress.ResetProgress();

		Assert.AreEqual(0, _progress.Collected);
		Assert.IsFalse(_progress.IsComplete);
	}

	[Test]
	public void StageEndsOnlyOnce()
	{
		Managers.Game.BeginStage();

		int endCount = 0;
		Define.StageResult seen = Define.StageResult.None;

		Managers.Game.OnStageEnded += result =>
		{
			endCount++;
			seen = result;
		};

		Managers.Game.ReportPlayerCaught();
		Managers.Game.ReportEscaped();

		Assert.AreEqual(1, endCount);
		Assert.AreEqual(Define.StageResult.Caught, seen);
		Assert.AreEqual(Define.StageResult.Caught, Managers.Game.Result);
	}

	[Test]
	public void IsPlayingIsFalseAfterTheStageEnds()
	{
		Managers.Game.BeginStage();
		Assert.IsTrue(Managers.Game.IsPlaying);

		Managers.Game.ReportEscaped();
		Assert.IsFalse(Managers.Game.IsPlaying);
	}

	[Test]
	public void PauseStopsPlayAndRestoresTimeScale()
	{
		Managers.Game.BeginStage();
		Managers.Game.SetPaused(true);

		Assert.IsTrue(Managers.Game.IsPaused);
		Assert.IsFalse(Managers.Game.IsPlaying);
		Assert.AreEqual(0.0f, Time.timeScale);

		Managers.Game.SetPaused(false);

		Assert.IsTrue(Managers.Game.IsPlaying);
		Assert.AreEqual(1.0f, Time.timeScale);
	}

	[Test]
	public void PauseIsIgnoredAfterTheStageEnds()
	{
		Managers.Game.BeginStage();
		Managers.Game.ReportEscaped();
		Managers.Game.TogglePause();

		Assert.IsFalse(Managers.Game.IsPaused);
	}
}
