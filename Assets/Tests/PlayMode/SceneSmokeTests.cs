using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SceneSmokeTests
{
	readonly List<string> _errors = new List<string>();

	[SetUp]
	public void SetUp()
	{
		_errors.Clear();
		Application.logMessageReceived += Record;
	}

	[TearDown]
	public void TearDown()
	{
		Application.logMessageReceived -= Record;
	}

	void Record(string message, string stackTrace, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
			_errors.Add($"{type}: {message}");
	}

	[UnityTest]
	public IEnumerator StageBootsWithoutErrors()
	{
		yield return QaScene.Load();

		for (int i = 0; i < 30; i++)
			yield return null;

		Assert.IsEmpty(_errors, $"씬 부팅 중 에러 로그 {_errors.Count}건:\n{string.Join("\n", _errors)}");
	}

	[UnityTest]
	public IEnumerator StageIsPlayableAfterBoot()
	{
		yield return QaScene.Load();

		Assert.IsTrue(Managers.Game.IsPlaying, "부팅 직후 스테이지가 진행 상태가 아니다");
		Assert.AreEqual(Define.StageResult.None, Managers.Game.Result);
		Assert.IsNotNull(Managers.Game.GetPlayer(), "GameManager에 플레이어가 등록되지 않았다");
	}

	[UnityTest]
	public IEnumerator EveryArtifactAndTheExitAreReachable()
	{
		yield return QaScene.Load();

		MapObjectPlacer placer = Object.FindFirstObjectByType<MapObjectPlacer>();
		Assert.IsNotNull(placer, "씬에 MapObjectPlacer가 없다");

		StageProgress progress = Object.FindFirstObjectByType<StageProgress>();
		Assert.IsNotNull(progress, "씬에 StageProgress가 없다");

		Assert.AreEqual(progress.Required, placer.Artifacts.Count,
			"배치된 유물 수가 클리어 조건과 다르다");

		Assert.IsNotNull(placer.ExitDoor, "출구가 배치되지 않았다");

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		Vector2Int start = MapCoord.WorldToTile(player.transform.position);
		Assert.IsTrue(MapCoord.IsWalkable(start.x, start.y),
			$"플레이어 시작 타일 ({start.x},{start.y})이 벽이다");

		int[] field = MapPathfinder.DistanceField(start.x, start.y);
		Assert.IsNotNull(field, "시작점에서 거리 필드를 만들지 못했다");

		foreach (Artifact artifact in placer.Artifacts)
		{
			Vector2Int tile = MapCoord.WorldToTile(artifact.transform.position);
			Assert.AreNotEqual(MapPathfinder.Unreachable, MapPathfinder.Sample(field, tile.x, tile.y),
				$"유물 {artifact.PointName}({tile.x},{tile.y})에 도달할 수 없다");
		}

		Vector2Int exit = MapCoord.WorldToTile(placer.ExitDoor.transform.position);
		Assert.AreNotEqual(MapPathfinder.Unreachable, MapPathfinder.Sample(field, exit.x, exit.y),
			$"출구({exit.x},{exit.y})에 도달할 수 없다");
	}

	[UnityTest]
	public IEnumerator EnemiesStartAwayFromThePlayer()
	{
		yield return QaScene.Load();

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		Vector2Int start = MapCoord.WorldToTile(player.transform.position);
		int[] field = MapPathfinder.DistanceField(start.x, start.y);

		List<string> tooClose = new List<string>();

		foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
		{
			Vector2Int tile = MapCoord.WorldToTile(enemy.transform.position);
			int distance = MapPathfinder.Sample(field, tile.x, tile.y);

			if (distance != MapPathfinder.Unreachable && distance < 12)
				tooClose.Add($"{enemy.name} at ({tile.x},{tile.y}) distance={distance}");
		}

		Assert.IsEmpty(tooClose, $"시작 지점에서 12칸 안에 적이 있다:\n{string.Join("\n", tooClose)}");
	}
}
