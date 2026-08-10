using UnityEngine;

public class SpawnSelector : MonoBehaviour
{
	public const string PlayerStartPoint = "player_start";

	MapPoint _playerStart;
	MapPoint _exitDoor;

	public MapPoint PlayerStart { get { return _playerStart; } }
	public MapPoint ExitDoor { get { return _exitDoor; } }

	public bool Select()
	{
		MapData map = Managers.Data.Map;

		MapPoint start = map == null ? null : map.Find(PlayerStartPoint);
		MapPoint exit = map == null ? null : map.Find(MapObjectPlacer.ExitDoorPoint);

		if (start == null)
		{
			Debug.LogError($"SpawnSelector: 맵에 {PlayerStartPoint} 가 없다");
			return false;
		}

		if (exit == null)
		{
			Debug.LogError($"SpawnSelector: 맵에 {MapObjectPlacer.ExitDoorPoint} 가 없다");
			return false;
		}

		_playerStart = start;
		_exitDoor = exit;
		return true;
	}

	public const float SpawnClearanceRadius = 0.35f;

	public static bool BlockedByDecoration(MapPoint point)
	{
		return point != null && BlockedByDecoration(MapCoord.ToWorld(point), SpawnClearanceRadius);
	}

	public static bool BlockedByDecoration(Vector3 world, float clearance)
	{
		MapData map = Managers.Data.Map;
		if (map == null || map.decorations == null)
			return false;

		foreach (MapDecoration deco in map.decorations)
		{
			if (deco.collisionEnabled == false
				|| deco.colliderWidth <= 0.0f || deco.colliderHeight <= 0.0f)
				continue;

			float centerX = deco.x + deco.width * 0.5f + deco.colliderOffsetX;
			float centerY = map.height - deco.y + deco.colliderOffsetY;

			if (Mathf.Abs(world.x - centerX) < deco.colliderWidth * 0.5f + clearance
				&& Mathf.Abs(world.y - centerY) < deco.colliderHeight * 0.5f + clearance)
				return true;
		}

		return false;
	}

	public Vector3 PlayerStartWorld()
	{
		return MapCoord.ToWorld(_playerStart);
	}

	public Vector3 ExitDoorWorld()
	{
		return MapCoord.ToWorld(_exitDoor);
	}
}
