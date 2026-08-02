using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

public class MapRenderProofTests
{
	const int Seed = 20260801;
	const int PixelsPerTile = 24;

	[UnityTest]
	public IEnumerator RenderWholeMapLit([Values(1, 2, 3)] int level)
	{
		Managers.Game.SetLevel(level);
		yield return QaScene.Load(Seed);

		MapData map = Managers.Data.Map;
		Assert.IsNotNull(map);

		foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include,
			FindObjectsSortMode.None))
		{
			if (light.lightType == Light2D.LightType.Global)
				light.intensity = 1.0f;
		}

		Camera camera = Camera.main;
		Assert.IsNotNull(camera, "메인 카메라가 없다");

		CameraController follow = Object.FindFirstObjectByType<CameraController>();
		if (follow != null)
			follow.enabled = false;

		camera.orthographic = true;
		camera.orthographicSize = map.height * 0.5f;
		camera.transform.position = new Vector3(map.width * 0.5f, map.height * 0.5f, -10.0f);

		int width = map.width * PixelsPerTile;
		int height = map.height * PixelsPerTile;

		RenderTexture target = new RenderTexture(width, height, 24);
		camera.targetTexture = target;
		camera.Render();

		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = target;

		Texture2D shot = new Texture2D(width, height, TextureFormat.RGB24, false);
		shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		shot.Apply();

		RenderTexture.active = previous;
		camera.targetTexture = null;

		string path = QaScene.WritePng($"ingame_l{Managers.Game.CurrentLevel}_seed{Managers.Data.LastSeed}.png", shot);

		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"level={Managers.Game.CurrentLevel} seed={Managers.Data.LastSeed} "
			+ $"fallback={Managers.Data.LastUsedFallback}");
		sb.AppendLine($"map={map.width}x{map.height} rooms={(map.rooms == null ? 0 : map.rooms.Length)}");

		MapPoint start = map.Find("player_start");
		MapPoint exit = map.Find(MapObjectPlacer.ExitDoorPoint);
		sb.AppendLine($"start=({start.col},{start.row}) exit=({exit.col},{exit.row})");

		MapDecoPlacer deco = Object.FindFirstObjectByType<MapDecoPlacer>();
		sb.AppendLine($"deco spawned={(deco == null ? 0 : deco.Count)}");

		foreach (Transform child in deco == null ? new Transform[0] : deco.GetComponentsInChildren<Transform>())
		{
			if (child.name.StartsWith("walldeco") == false)
				continue;

			Vector2Int tile = MapCoord.WorldToTile(child.position);
			sb.AppendLine($"  {child.name} world={child.position} tile=({tile.x},{tile.y}) "
				+ $"wallGid={map.GetGid(map.walls, tile.x, tile.y)}");
		}

		PlayerController player = Object.FindFirstObjectByType<PlayerController>();
		if (player != null)
		{
			Vector2Int tile = MapCoord.WorldToTile(player.transform.position);
			sb.AppendLine($"player world={player.transform.position} tile=({tile.x},{tile.y}) "
				+ $"walkable={MapCoord.IsWalkable(tile.x, tile.y)} "
				+ $"wallGid={map.GetGid(map.walls, tile.x, tile.y)}");
		}

		QaScene.WriteReport($"ingame_render_l{level}.txt", sb.ToString());
		Debug.Log($"MapRenderProof: {path}\n{sb}");

		Object.Destroy(target);
		yield return null;
	}
}
