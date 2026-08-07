using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DirectionalSpriteAnimator : MonoBehaviour
{
	public const string StateIdle = "idle";
	public const string StateWalk = "walk";
	public const string StateChase = "chase";
	public const string StateRun = "run";

	static readonly string[] Fallback = { StateChase, StateRun, StateWalk, StateIdle };

	[SerializeField]
	string _characterKey;

	[SerializeField]
	SpriteRenderer _renderer;

	[SerializeField]
	float _defaultFps = 6.0f;

	[SerializeField, Range(0.0f, 0.9f)]
	float _stutter = 0.0f;

	[SerializeField]
	float _twitchPixels = 0.0f;

	[SerializeField]
	int _pixelsPerUnit = 32;

	CharacterSpec _spec;
	CharacterState _state;
	readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

	string _direction = "s";
	float _fps;
	float _timer;
	int _frame;
	float _intensity = 1.0f;
	Vector3 _restLocalPosition;
	bool _restCaptured;

	public string CharacterKey { get { return _characterKey; } }
	public CharacterSpec Spec { get { return _spec; } }

	void Awake()
	{
		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		if (string.IsNullOrEmpty(_characterKey))
			return;

		_spec = CharacterCatalog.Get(_characterKey);

		if (_spec == null)
		{
			Debug.LogError($"DirectionalSpriteAnimator: 캐릭터 '{_characterKey}' 가 카탈로그에 없다");
			return;
		}

		SetState(StateIdle);
	}

	public void UseRenderer(SpriteRenderer renderer)
	{
		if (renderer == null)
			return;

		_renderer = renderer;
		_restCaptured = false;
	}

	public void SetCharacter(string key)
	{
		_characterKey = key;
		_spec = CharacterCatalog.Get(key);
		_sprites.Clear();
		_state = null;
		SetState(StateIdle);
	}

	public void SetHeading(Vector2 heading)
	{
		if (heading.sqrMagnitude <= 0.0001f)
			return;

		string next = CharacterCatalog.DirectionFrom(heading);
		if (next == _direction)
			return;

		_direction = next;
		Apply();
	}

	public void SetState(string state)
	{
		if (_spec == null)
			return;

		CharacterState resolved = Resolve(state);
		if (resolved == null || resolved == _state)
			return;

		_state = resolved;
		_frame = 0;
		_timer = 0.0f;
		_fps = resolved.fps > 0.0f ? resolved.fps : _defaultFps;

		Apply();
	}

	public void SetFps(float fps)
	{
		if (fps > 0.0f)
			_fps = fps;
	}

	public float FpsFor(string state, string movement)
	{
		CharacterState resolved = Resolve(state);
		if (resolved == null)
			return _defaultFps;

		switch (movement)
		{
			case "sneak": return resolved.fpsSneak > 0.0f ? resolved.fpsSneak : resolved.fps;
			case "run": return resolved.fpsRun > 0.0f ? resolved.fpsRun : resolved.fps;
			default: return resolved.fpsWalk > 0.0f ? resolved.fpsWalk : resolved.fps;
		}
	}

	CharacterState Resolve(string state)
	{
		CharacterState direct = _spec.State(state);
		if (direct != null)
			return direct;

		foreach (string name in Fallback)
		{
			CharacterState found = _spec.State(name);
			if (found != null)
				return found;
		}

		return null;
	}

	public void SetIntensity(float intensity)
	{
		_intensity = Mathf.Clamp(intensity, 0.0f, 3.0f);
	}

	public void SetStutter(float stutter, float twitchPixels)
	{
		_stutter = Mathf.Clamp(stutter, 0.0f, 0.9f);
		_twitchPixels = Mathf.Max(0.0f, twitchPixels);
	}

	float HoldSeconds(int frame)
	{
		float step = 1.0f / _fps;

		if (_stutter <= 0.0f || _state == null || _state.frames <= 1)
			return step;

		float phase = Mathf.Repeat(frame * 0.6180339887f, 1.0f);
		float scale = Mathf.Lerp(1.0f - _stutter, 1.0f + _stutter, phase);
		return step * scale;
	}

	void Update()
	{
		UpdateTwitch();

		if (_state == null || _state.frames <= 1 || _fps <= 0.0f)
			return;

		_timer += Time.deltaTime * _intensity;

		float hold = HoldSeconds(_frame);
		if (_timer < hold)
			return;

		_timer -= hold;
		_frame = (_frame + 1) % _state.frames;
		Apply();
	}

	void UpdateTwitch()
	{
		if (_renderer == null)
			return;

		Transform art = _renderer.transform;

		if (art == transform)
			return;

		if (_restCaptured == false)
		{
			_restLocalPosition = art.localPosition;
			_restCaptured = true;
		}

		if (_twitchPixels <= 0.0f)
		{
			art.localPosition = _restLocalPosition;
			return;
		}

		float unit = 1.0f / Mathf.Max(1, _pixelsPerUnit);
		float seed = Time.time * 17.0f;

		int x = Mathf.RoundToInt((Mathf.PerlinNoise(seed, 0.0f) * 2.0f - 1.0f) * _twitchPixels);
		int y = Mathf.RoundToInt((Mathf.PerlinNoise(0.0f, seed) * 2.0f - 1.0f) * _twitchPixels);

		art.localPosition = _restLocalPosition + new Vector3(x * unit, y * unit, 0.0f);
	}

	void Apply()
	{
		if (_renderer == null || _spec == null || _state == null)
			return;

		Sprite sprite = Load(CharacterCatalog.SpriteName(_spec.key, _state.name, _direction, _frame));
		if (sprite != null)
			_renderer.sprite = sprite;
	}

	Sprite Load(string spriteName)
	{
		Sprite cached;
		if (_sprites.TryGetValue(spriteName, out cached))
			return cached;

		if (_sprites.Count == 0)
			LoadAllSheets();

		return _sprites.TryGetValue(spriteName, out cached) ? cached : null;
	}

	void LoadAllSheets()
	{
		foreach (CharacterState state in _spec.states)
		{
			foreach (Sprite sprite in Resources.LoadAll<Sprite>(state.resource))
				_sprites[sprite.name] = sprite;
		}

		if (_sprites.Count == 0)
			Debug.LogError($"DirectionalSpriteAnimator: {_spec.key} 스프라이트를 못 읽었다");
	}
}
