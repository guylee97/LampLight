using NUnit.Framework;
using UnityEngine;

public class InteractableTests
{
	GameObject _progressHost;
	StageProgress _progress;

	[SetUp]
	public void SetUp()
	{
		Managers.Game.Clear();
		Managers.Game.BeginStage();

		_progressHost = new GameObject("StageProgressHost");
		_progress = _progressHost.AddComponent<StageProgress>();
	}

	[TearDown]
	public void TearDown()
	{
		Object.DestroyImmediate(_progressHost);
		Managers.Game.Clear();
	}

	static Artifact MakeArtifact()
	{
		GameObject go = new GameObject("Artifact");
		go.AddComponent<CircleCollider2D>();
		return go.AddComponent<Artifact>();
	}

	static Altar MakeAltar()
	{
		GameObject go = new GameObject("Altar");
		go.AddComponent<BoxCollider2D>();
		return go.AddComponent<Altar>();
	}

	[Test]
	public void ArtifactReportsToProgressExactlyOnce()
	{
		Artifact artifact = MakeArtifact();
		artifact.Init(_progress, "artifact_1");

		int raised = 0;
		artifact.OnCollected += _ => raised++;

		Assert.IsTrue(artifact.TryCollect());
		Assert.IsFalse(artifact.TryCollect());

		Assert.AreEqual(1, raised);
		Assert.AreEqual(1, _progress.Collected);
		Assert.IsTrue(artifact.IsCollected);

		Object.DestroyImmediate(artifact.gameObject);
	}

	[Test]
	public void CollectedArtifactStopsBeingInteractableAndHides()
	{
		Artifact artifact = MakeArtifact();
		artifact.Init(_progress, "artifact_1");

		Assert.IsTrue(artifact.CanInteract);

		artifact.TryCollect();

		Assert.IsFalse(artifact.CanInteract);
		Assert.IsFalse(artifact.gameObject.activeSelf);

		Object.DestroyImmediate(artifact.gameObject);
	}

	[Test]
	public void ArtifactInteractSurvivesAMissingPlayer()
	{
		Artifact artifact = MakeArtifact();
		artifact.Init(_progress, "artifact_1");

		artifact.Interact(null);

		Assert.IsTrue(artifact.IsCollected);
		Assert.AreEqual(1, _progress.Collected);

		Object.DestroyImmediate(artifact.gameObject);
	}

	[Test]
	public void AltarRefusesArtifactsThatWereNeverCollected()
	{
		Altar altar = MakeAltar();
		altar.Init(_progress);

		Assert.IsFalse(altar.CanInteract, "들고 있는 유물이 없으면 올릴 수 없다");

		_progress.ReportCollected();

		Assert.IsTrue(altar.CanInteract, "유물을 하나 주웠으면 올릴 수 있다");

		Object.DestroyImmediate(altar.gameObject);
	}

	[Test]
	public void AltarSealsOnlyAfterEveryRequiredArtifact()
	{
		Altar altar = MakeAltar();
		altar.Init(_progress);

		for (int i = 0; i < _progress.Required; i++)
			_progress.ReportCollected();

		for (int i = 0; i < _progress.Required - 1; i++)
		{
			altar.Interact(null);
			Assert.IsFalse(altar.IsSealed, $"{i + 1}개만 올렸는데 봉인됐다");
			Assert.AreEqual(Define.StageResult.None, Managers.Game.Result);
		}

		altar.Interact(null);

		Assert.IsTrue(altar.IsSealed);
		Assert.AreEqual(Define.StageResult.Cleared, Managers.Game.Result);

		Object.DestroyImmediate(altar.gameObject);
	}

	[Test]
	public void SealedAltarClearsTheStageOnlyOnce()
	{
		Altar altar = MakeAltar();
		altar.Init(_progress);

		int sealed_ = 0;
		altar.OnSealed += () => sealed_++;

		for (int i = 0; i < _progress.Required; i++)
			_progress.ReportCollected();

		for (int i = 0; i < _progress.Required + 2; i++)
			altar.Interact(null);

		Assert.AreEqual(1, sealed_, "봉인 이벤트는 한 번만");
		Assert.AreEqual(Define.StageResult.Cleared, Managers.Game.Result);

		Object.DestroyImmediate(altar.gameObject);
	}
}
