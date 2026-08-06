using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering.Universal;

public class DebugOverlay : MonoBehaviour
{
	public const float RevealIntensity = 1.0f;

	public static bool Invulnerable { get; private set; }

	public static void SetInvulnerableForTests(bool value)
	{
		SetInvulnerable(value);
	}

	static void SetInvulnerable(bool value)
	{
		Invulnerable = value;
		Physics2D.IgnoreLayerCollision((int)Define.Layer.Player, (int)Define.Layer.Enemy, value);
	}

	[SerializeField]
	bool _enabledInBuild = true;

	[SerializeField]
	float _panelWidth = 520.0f;

	[SerializeField]
	float _legendWidth = 240.0f;

	static readonly Color ActiveColor = new Color(0.45f, 1.0f, 0.6f);
	static readonly Color InvulnerableColor = new Color(0.45f, 1.0f, 0.6f);

	struct LegendRow
	{
		public string Text;
		public System.Func<bool> On;
	}

	static DebugOverlay _instance;

	static readonly LegendRow[] Legend =
	{
		new LegendRow { Text = "1   Info panel", On = () => _instance != null && _instance._panel },
		new LegendRow { Text = "2   Reveal map", On = () => _instance != null && _instance._revealed },
		new LegendRow { Text = "3   Map overlay", On = () => _instance != null && _instance._overlay },
		new LegendRow { Text = "4   Teleport", On = null },
		new LegendRow { Text = "G   Invulnerable", On = () => Invulnerable },
		new LegendRow { Text = "T   Refill fuel", On = null },
		new LegendRow { Text = "R   Rebuild map", On = null },
	};

	static readonly Color RoomColor = new Color(0.35f, 0.75f, 1.0f, 0.9f);
	static readonly Color ArtifactColor = new Color(1.0f, 0.35f, 0.85f, 1.0f);
	static readonly Color ExitColor = new Color(1.0f, 0.72f, 0.2f, 1.0f);
	static readonly Color SpawnColor = new Color(0.5f, 1.0f, 0.5f, 0.8f);
	static readonly Color EnemyColor = new Color(1.0f, 0.3f, 0.25f, 1.0f);
	static readonly Color NoiseColor = new Color(0.9f, 0.9f, 0.4f, 0.7f);

	bool _panel;
	bool _revealed;
	bool _overlay;
	float _savedIntensity = -1.0f;

	Light2D _global;
	Material _lines;
	GUIStyle _style;
	GUIStyle _legendStyle;

	PlayerController _player;
	StageProgress _progress;
	int _artifactCursor;

	void Awake()
	{
		_instance = this;

		if (_enabledInBuild == false && Debug.isDebugBuild == false && Application.isEditor == false)
			enabled = false;
	}

	void Update()
	{
		Keyboard keyboard = Keyboard.current;
		if (keyboard == null)
			return;

		if (Pressed(keyboard.digit1Key, keyboard.numpad1Key))
			_panel = !_panel;

		if (Pressed(keyboard.digit2Key, keyboard.numpad2Key))
			ToggleReveal();

		if (Pressed(keyboard.digit3Key, keyboard.numpad3Key))
			_overlay = !_overlay;

		if (Pressed(keyboard.digit4Key, keyboard.numpad4Key))
			TeleportToNextArtifact();

		if (keyboard.gKey.wasPressedThisFrame)
			SetInvulnerable(Invulnerable == false);

		if (keyboard.tKey.wasPressedThisFrame)
			RefillLamp();

		if (keyboard.rKey.wasPressedThisFrame)
			Managers.Scene.LoadScene(Define.Scene.InGame);
	}

	static bool Pressed(KeyControl primary, KeyControl secondary)
	{
		return (primary != null && primary.wasPressedThisFrame)
			|| (secondary != null && secondary.wasPressedThisFrame);
	}

	void RefillLamp()
	{
		GameObject player = Managers.Game.GetPlayer();
		if (player == null)
			return;

		Lamp lamp = player.GetComponentInChildren<Lamp>();
		if (lamp != null)
			lamp.Refill(lamp.MaxDuration);
	}

	void ToggleReveal()
	{
		if (_global == null)
			_global = FindGlobalLight();

		if (_global == null)
			return;

		_revealed = !_revealed;

		if (_revealed)
		{
			if (_savedIntensity < 0.0f)
				_savedIntensity = _global.intensity;

			_global.intensity = RevealIntensity;
			return;
		}

		if (_savedIntensity >= 0.0f)
			_global.intensity = _savedIntensity;
	}

	static Light2D FindGlobalLight()
	{
		foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsInactive.Include,
			FindObjectsSortMode.None))
		{
			if (light.lightType == Light2D.LightType.Global)
				return light;
		}

		return null;
	}

	void TeleportToNextArtifact()
	{
		MapData map = Managers.Data.Map;
		GameObject player = Managers.Game.GetPlayer();

		if (map == null || map.objects == null || player == null)
			return;

		List<MapPoint> targets = new List<MapPoint>();
		foreach (MapPoint point in map.objects)
		{
			if (point.name.StartsWith(MapObjectPlacer.ArtifactPrefix)
				|| point.name == MapObjectPlacer.ExitDoorPoint)
				targets.Add(point);
		}

		if (targets.Count == 0)
			return;

		_artifactCursor = (_artifactCursor + 1) % targets.Count;

		Vector3 destination = MapCoord.ToWorld(targets[_artifactCursor]);

		PlayerController controller = player.GetComponent<PlayerController>();
		if (controller != null)
			controller.Teleport(destination);
		else
			player.transform.position = destination;
	}

	void OnGUI()
	{
		EnsureStyles();
		DrawKeyLegend();
		DrawRoundButtons();

		if (Invulnerable)
		{
			GUI.color = InvulnerableColor;
			GUI.Label(new Rect(Screen.width - _legendWidth - 18.0f, 10.0f, _legendWidth, 28.0f),
				"INVULNERABLE", _legendStyle);
			GUI.color = Color.white;
		}

		if (_panel == false)
			return;

		string text = BuildReport();
		float height = _style.CalcHeight(new GUIContent(text), _panelWidth) + 16.0f;

		GUI.Box(new Rect(8, 8, _panelWidth, height), GUIContent.none);
		GUI.Label(new Rect(16, 12, _panelWidth - 16, height), text, _style);
	}

	void DrawRoundButtons()
	{
		float width = 110.0f;
		float height = 34.0f;
		float gap = 6.0f;
		float x = Screen.width - width - 8.0f;
		float y = (Invulnerable ? 44.0f : 8.0f) + Legend.Length * 24.0f + 24.0f;

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			if (GUI.Button(new Rect(x, y, width, height), $"ROUND {level}"))
				StartRound(level);

			y += height + gap;
		}
	}

	void StartRound(int level)
	{
		Managers.Game.SetLevel(level);
		Managers.Scene.LoadScene(Define.Scene.InGame);
	}

	void EnsureStyles()
	{
		if (_style == null)
		{
			_style = new GUIStyle(GUI.skin.label);
			_style.fontSize = 18;
			_style.richText = false;
			_style.wordWrap = false;
		}

		if (_legendStyle == null)
		{
			_legendStyle = new GUIStyle(GUI.skin.label);
			_legendStyle.fontSize = 18;
			_legendStyle.alignment = TextAnchor.UpperRight;
			_legendStyle.richText = false;
			_legendStyle.wordWrap = false;
		}
	}

	void DrawKeyLegend()
	{
		float x = Screen.width - _legendWidth - 8.0f;
		float y = Invulnerable ? 44.0f : 8.0f;
		float height = Legend.Length * 24.0f + 16.0f;

		GUI.Box(new Rect(x, y, _legendWidth, height), GUIContent.none);

		for (int i = 0; i < Legend.Length; i++)
		{
			GUI.color = Legend[i].On == null || Legend[i].On() == false
				? new Color(0.78f, 0.78f, 0.80f)
				: ActiveColor;

			GUI.Label(new Rect(x - 10.0f, y + 8.0f + i * 24.0f, _legendWidth, 24.0f),
				Legend[i].Text, _legendStyle);
		}

		GUI.color = Color.white;
	}

	string BuildReport()
	{
		StringBuilder sb = new StringBuilder();


		MapData map = Managers.Data.Map;
		if (map == null)
		{
			sb.AppendLine("no map");
			return sb.ToString();
		}

		sb.AppendLine($"level {Managers.Game.CurrentLevel}   seed {Managers.Data.LastSeed}"
			+ $"{(Managers.Data.LastUsedFallback ? " (bake fallback)" : "")}");
		sb.AppendLine($"map {map.width}x{map.height}   rooms {(map.rooms == null ? 0 : map.rooms.Length)}   "
			+ $"deco {DecoCount()}");

		if (_progress == null)
			_progress = FindFirstObjectByType<StageProgress>();

		if (_progress != null)
			sb.AppendLine($"artifacts {_progress.Collected}/{_progress.Required}   "
				+ $"weight {_progress.WeightedValue:F2}");

		if (_player == null)
			_player = FindFirstObjectByType<PlayerController>();

		if (_player != null)
		{
			Vector3 position = _player.transform.position;
			Vector2Int tile = MapCoord.WorldToTile(position);

			sb.AppendLine();
			sb.AppendLine($"player ({position.x:F2}, {position.y:F2})  tile ({tile.x},{tile.y})");
			sb.AppendLine($"  noise {_player.CurrentNoiseRadius:F2}   move {(_player.IsSneaking ? "sneak" : "walk")}"
				+ $"   noisy floor {(MapCoord.IsNoisy(position) ? "yes" : "no")}");
			sb.AppendLine($"  tile {(MapCoord.IsWalkable(tile.x, tile.y) ? "open" : "WALL !!")}"
				+ $"   wall gid {map.GetGid(map.walls, tile.x, tile.y)}");
		}

		EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
		sb.AppendLine();
		sb.AppendLine($"enemies {enemies.Length}");

		for (int i = 0; i < enemies.Length && i < 8; i++)
		{
			EnemyBase enemy = enemies[i];
			float distance = _player == null
				? -1.0f
				: Vector2.Distance(enemy.transform.position, _player.transform.position);

			sb.AppendLine($"  {enemy.name,-16} {enemy.State,-8} dist {distance:F1}");
		}

		return sb.ToString();
	}

	int DecoCount()
	{
		MapDecoPlacer placer = FindFirstObjectByType<MapDecoPlacer>();
		return placer == null ? 0 : placer.Count;
	}

	void OnRenderObject()
	{
		if (_overlay == false)
			return;

		MapData map = Managers.Data.Map;
		if (map == null)
			return;

		EnsureLineMaterial();

		_lines.SetPass(0);
		GL.PushMatrix();
		GL.Begin(GL.LINES);

		DrawRooms(map);
		DrawPoints(map);
		DrawEnemies();
		DrawPlayerNoise();

		GL.End();
		GL.PopMatrix();
	}

	void EnsureLineMaterial()
	{
		if (_lines != null)
			return;

		Shader shader = Shader.Find("Hidden/Internal-Colored");
		_lines = new Material(shader);
		_lines.hideFlags = HideFlags.HideAndDontSave;
		_lines.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		_lines.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		_lines.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		_lines.SetInt("_ZWrite", 0);
		_lines.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
	}

	void DrawRooms(MapData map)
	{
		if (map.rooms == null)
			return;

		GL.Color(RoomColor);

		foreach (MapRoom room in map.rooms)
		{
			float left = room.col;
			float right = room.col + room.width;
			float top = map.height - room.row;
			float bottom = map.height - (room.row + room.height);

			Line(left, bottom, right, bottom);
			Line(right, bottom, right, top);
			Line(right, top, left, top);
			Line(left, top, left, bottom);
		}
	}

	void DrawPoints(MapData map)
	{
		if (map.objects != null)
		{
			foreach (MapPoint point in map.objects)
			{
				Color color = point.name == MapObjectPlacer.ExitDoorPoint ? ExitColor : ArtifactColor;
				if (point.name.StartsWith(MapObjectPlacer.ArtifactPrefix) == false
					&& point.name != MapObjectPlacer.ExitDoorPoint)
					color = SpawnColor;

				GL.Color(color);
				Cross(MapCoord.ToWorld(point), 0.4f);
			}
		}

		if (map.spawns == null)
			return;

		GL.Color(SpawnColor);
		foreach (MapPoint point in map.spawns)
			Cross(MapCoord.ToWorld(point), 0.25f);
	}

	void DrawEnemies()
	{
		GL.Color(EnemyColor);

		foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
		{
			Vector3 position = enemy.transform.position;
			Cross(position, 0.5f);

			PingScheduler ping = enemy.GetComponent<PingScheduler>();
			if (ping != null && ping.RadiusTiles > 0.0f)
				Circle(position, ping.RadiusTiles, 24);
		}
	}

	void DrawPlayerNoise()
	{
		if (_player == null || _player.CurrentNoiseRadius <= 0.0f)
			return;

		GL.Color(NoiseColor);
		Circle(_player.transform.position, _player.CurrentNoiseRadius, 32);
	}

	static void Line(float x0, float y0, float x1, float y1)
	{
		GL.Vertex3(x0, y0, 0.0f);
		GL.Vertex3(x1, y1, 0.0f);
	}

	static void Cross(Vector3 center, float size)
	{
		Line(center.x - size, center.y, center.x + size, center.y);
		Line(center.x, center.y - size, center.x, center.y + size);
	}

	static void Circle(Vector3 center, float radius, int segments)
	{
		float step = Mathf.PI * 2.0f / segments;

		for (int i = 0; i < segments; i++)
		{
			float a = i * step;
			float b = (i + 1) * step;

			Line(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius,
				center.x + Mathf.Cos(b) * radius, center.y + Mathf.Sin(b) * radius);
		}
	}

	void OnDestroy()
	{
		if (Invulnerable)
			SetInvulnerable(false);

		if (_instance == this)
			_instance = null;

		if (_lines != null)
			DestroyImmediate(_lines);
	}
}
