using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SceneSmokeTests
{
	const int SafeStartDistance = 7;

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

		LevelConfig config = Managers.Game.Level;

		int stashed = 0;
		foreach (Container container in Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
		{
			if (container.HoldsArtifact)
				stashed++;
		}

		Assert.AreEqual(config.ArtifactsPlaced, placer.Artifacts.Count + stashed,
			"배치된 유물 수가 레벨 정의와 다르다 (컨테이너에 숨긴 것 포함)");

		Assert.GreaterOrEqual(placer.Artifacts.Count + stashed, progress.Required,
			"배치 수가 필요 수보다 적으면 클리어가 불가능하다");

		Assert.IsNotNull(placer.Altar, "제단이 배치되지 않았다");

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

		foreach (Container container in Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
		{
			if (container.HoldsArtifact == false)
				continue;

			Vector2Int box = MapCoord.WorldToTile(container.transform.position);
			Assert.IsTrue(HasReachableSpotBeside(field, box),
				$"유물이 든 상자({box.x},{box.y}) 옆에 설 수 있는 칸이 없다");
		}

		Vector2Int altar = MapCoord.WorldToTile(placer.Altar.transform.position);
		Assert.AreNotEqual(MapPathfinder.Unreachable, MapPathfinder.Sample(field, altar.x, altar.y),
			$"제단({altar.x},{altar.y})에 도달할 수 없다");
	}

	const int InteractTiles = 1;

	static bool HasReachableSpotBeside(int[] field, Vector2Int tile)
	{
		for (int dy = -InteractTiles; dy <= InteractTiles; dy++)
		{
			for (int dx = -InteractTiles; dx <= InteractTiles; dx++)
			{
				int col = tile.x + dx;
				int row = tile.y + dy;

				if (MapCoord.IsPassable(col, row) == false)
					continue;

				if (MapPathfinder.Sample(field, col, row) != MapPathfinder.Unreachable)
					return true;
			}
		}

		return false;
	}

	[UnityTest]
	public IEnumerator TheListenerSitsOnThePlayer()
	{
		yield return QaScene.Load();

		AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(
			FindObjectsInactive.Include, FindObjectsSortMode.None);

		Assert.AreEqual(1, listeners.Length, "AudioListener는 정확히 하나여야 한다");

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		Assert.IsNotNull(player);

		Assert.AreEqual(player.gameObject, listeners[0].gameObject,
			"리스너가 플레이어에 없으면 카메라 Z오프셋만큼 거리가 더해져 소리가 죽는다");

		float gap = Vector3.Distance(listeners[0].transform.position, player.transform.position);
		Assert.Less(gap, 0.01f, $"리스너가 플레이어에서 {gap:0.00} 떨어져 있다");
	}

	[UnityTest]
	public IEnumerator EnemiesStartAwayFromThePlayer()
	{
		yield return QaScene.Load();

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		Vector2Int start = MapCoord.WorldToTile(player.transform.position);
		int[] field = MapPathfinder.DistanceField(start.x, start.y);

		List<string> tooClose = new List<string>();

		EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
		LevelConfig config = LevelTable.Get(Managers.Game.CurrentLevel);
		int expected = config.YokaiCount;

		Assert.AreEqual(expected, enemies.Length,
			$"L{Managers.Game.CurrentLevel} 요괴가 {expected}마리 나와야 하는데 {enemies.Length}마리다");

		foreach (EnemyBase enemy in enemies)
		{
			Vector2Int tile = MapCoord.WorldToTile(enemy.transform.position);
			int distance = MapPathfinder.Sample(field, tile.x, tile.y);

			if (distance != MapPathfinder.Unreachable && distance < SafeStartDistance)
				tooClose.Add($"{enemy.name} at ({tile.x},{tile.y}) distance={distance}");
		}

		Assert.IsEmpty(tooClose,
			$"시작 지점에서 {SafeStartDistance}칸 안에 적이 있다:\n{string.Join("\n", tooClose)}");
	}
}
