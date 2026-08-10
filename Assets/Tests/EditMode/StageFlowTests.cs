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
	public void NewGameReturnsToTheFirstLevelAfterTheLastOneWasCleared()
	{
		Managers.Game.SetLevel(LevelTable.MaxLevel);
		Managers.Game.ReportEscaped();

		Managers.Game.NewGame();

		Assert.AreEqual(LevelTable.MinLevel, Managers.Game.CurrentLevel,
			"마지막 전각을 깬 뒤 새로 시작하면 1전각부터여야 한다");
	}

	[Test]
	public void NewGameClearsTheFailureStreakThatLengthensTheLamp()
	{
		Managers.Game.GameOver();
		Managers.Game.BeginStage();
		Managers.Game.GameOver();
		Managers.Game.BeginStage();
		Managers.Game.GameOver();

		Managers.Game.NewGame();

		Assert.AreEqual(0, Managers.Game.ConsecutiveFailures,
			"새 게임이 직전 판의 연패를 물려받으면 등불이 1.2배로 늘어난 채 시작한다");
	}

	[Test]
	public void AdvancingKeepsTheLevelAcrossASceneLoad()
	{
		Managers.Game.SetLevel(LevelTable.MinLevel);
		Managers.Game.AdvanceLevel();

		Managers.Game.Clear();

		Assert.AreEqual(LevelTable.MinLevel + 1, Managers.Game.CurrentLevel,
			"씬 전환마다 도는 Clear 가 레벨을 되돌리면 전각 진행이 불가능하다");
	}

	[Test]
	public void BeginStageRestoresTimeScaleAfterGameOverFroze()
	{
		Time.timeScale = 0.0f;

		Managers.Game.BeginStage();

		Assert.AreEqual(1.0f, Time.timeScale, 0.0001f,
			"게임오버가 timeScale 을 0 으로 두고 재시작하면 새 스테이지가 멈춘 채 시작한다");
	}

	[Test]
	public void ResumingSyncsTimeScaleEvenWhenPauseFlagUnchanged()
	{
		Managers.Game.SetPaused(false);
		Time.timeScale = 0.0f;

		Managers.Game.SetPaused(false);

		Assert.AreEqual(1.0f, Time.timeScale, 0.0001f,
			"IsPaused 가 이미 false 여도 timeScale 은 되돌려야 한다");
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
	public void ExtraCollectsKeepCountingForScore()
	{
		int completions = 0;
		_progress.OnAllArtifactsCollected += () => completions++;

		for (int i = 0; i < _progress.Required + 5; i++)
			_progress.ReportCollected();

		Assert.AreEqual(_progress.Required + 5, _progress.Collected);
		Assert.AreEqual(1, completions);
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

		Managers.Game.ReportEscaped();
		Managers.Game.ReportEscaped();

		Assert.AreEqual(1, endCount);
		Assert.AreEqual(Define.StageResult.Cleared, seen);
		Assert.AreEqual(Define.StageResult.Cleared, Managers.Game.Result);
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
