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

	CharacterSpec _spec;
	CharacterState _state;
	readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

	string _direction = "s";
	float _fps;
	float _timer;
	int _frame;

	public string CharacterKey { get { return _characterKey; } }
	public CharacterSpec Spec { get { return _spec; } }

	void Awake()
	{
		if (_renderer == null)
			_renderer = GetComponent<SpriteRenderer>();

		_spec = CharacterCatalog.Get(_characterKey);

		if (_spec == null)
		{
			Debug.LogError($"DirectionalSpriteAnimator: 캐릭터 '{_characterKey}' 가 카탈로그에 없다");
			return;
		}

		SetState(StateIdle);
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

	void Update()
	{
		if (_state == null || _state.frames <= 1 || _fps <= 0.0f)
			return;

		_timer += Time.deltaTime;

		float step = 1.0f / _fps;
		if (_timer < step)
			return;

		_timer -= step;
		_frame = (_frame + 1) % _state.frames;
		Apply();
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
