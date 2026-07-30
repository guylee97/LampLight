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

	static ExitDoor MakeDoor()
	{
		GameObject go = new GameObject("ExitDoor");
		go.AddComponent<BoxCollider2D>();
		return go.AddComponent<ExitDoor>();
	}

	static OilCanister MakeCanister()
	{
		GameObject go = new GameObject("OilCanister");
		go.AddComponent<BoxCollider2D>();
		return go.AddComponent<OilCanister>();
	}

	[Test]
	public void OilCanisterIgnoresAMissingPlayer()
	{
		OilCanister canister = MakeCanister();

		canister.Interact(null);

		Assert.IsFalse(canister.IsUsed);
		Assert.IsTrue(canister.CanInteract);

		Object.DestroyImmediate(canister.gameObject);
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
	public void ExitDoorStaysShutUntilEveryArtifactIsCollected()
	{
		ExitDoor door = MakeDoor();
		door.Init(_progress);

		Assert.IsFalse(door.IsOpen);

		for (int i = 0; i < _progress.Required - 1; i++)
			_progress.ReportCollected();

		Assert.IsFalse(door.IsOpen);

		_progress.ReportCollected();

		Assert.IsTrue(door.IsOpen);

		Object.DestroyImmediate(door.gameObject);
	}

	[Test]
	public void ShutExitDoorDoesNotEndTheStage()
	{
		ExitDoor door = MakeDoor();
		door.Init(_progress);

		door.Interact(null);

		Assert.AreEqual(Define.StageResult.None, Managers.Game.Result);

		Object.DestroyImmediate(door.gameObject);
	}

	[Test]
	public void OpenExitDoorClearsTheStageOnlyOnce()
	{
		ExitDoor door = MakeDoor();
		door.Init(_progress);

		int escapes = 0;
		door.OnEscaped += () => escapes++;

		for (int i = 0; i < _progress.Required; i++)
			_progress.ReportCollected();

		door.Interact(null);
		door.Interact(null);

		Assert.AreEqual(1, escapes);
		Assert.AreEqual(Define.StageResult.Cleared, Managers.Game.Result);

		Object.DestroyImmediate(door.gameObject);
	}


}
