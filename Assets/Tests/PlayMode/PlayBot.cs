using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class PlayBot
{
	const float StuckSampleInterval = 0.4f;
	const float StuckMinDistance = 0.05f;
	const float StuckLimit = 1.2f;
	const float AxisThreshold = 0.04f;

	readonly Transform _actor;
	readonly Collider2D _body;
	readonly Keyboard _keyboard;
	readonly List<Key> _keys = new List<Key>(4);
	readonly RaycastHit2D[] _casts = new RaycastHit2D[8];

	public bool Arrived { get; private set; }
	public string Failure { get; private set; }
	public Vector2 StoppedAt { get; private set; }

	public PlayBot(Transform actor)
	{
		_actor = actor;
		_body = actor.GetComponent<Collider2D>();
		_keyboard = InputSystem.AddDevice<Keyboard>("QaKeyboard");
	}

	public string DescribeBlockers(Vector2 direction)
	{
		if (_body == null)
			return "콜라이더 없음";

		ContactFilter2D filter = new ContactFilter2D();
		filter.useTriggers = false;

		int count = _body.Cast(direction.normalized, filter, _casts, 0.5f);
		if (count == 0)
			return "막은 콜라이더 없음";

		List<string> found = new List<string>();
		for (int i = 0; i < count; i++)
		{
			Collider2D hit = _casts[i].collider;
			found.Add($"{hit.name}(layer {hit.gameObject.layer}, {_casts[i].distance:0.00}만큼 앞)");
		}

		return string.Join(" / ", found);
	}

	public void Dispose()
	{
		Release();

		if (_keyboard != null && _keyboard.added)
			InputSystem.RemoveDevice(_keyboard);
	}

	public void Release()
	{
		Send(new KeyboardState());
	}

	public void Press(Key key)
	{
		Send(new KeyboardState(key));
	}

	void Send(KeyboardState state)
	{
		InputSystem.QueueStateEvent(_keyboard, state);
		InputSystem.Update();
	}

	public IEnumerator Tap(Key key)
	{
		Press(key);
		yield return null;
		yield return null;
		Release();
		yield return null;
	}

	public IEnumerator WalkTo(Vector2 target, float arriveRadius, float timeout)
	{
		Arrived = false;
		Failure = null;

		float deadline = Time.time + timeout;
		float nextSample = Time.time + StuckSampleInterval;
		float stuckFor = 0;
		Vector2 lastSample = _actor.position;

		while (Time.time < deadline)
		{
			Vector2 position = _actor.position;
			StoppedAt = position;

			Vector2 delta = target - position;
			if (delta.magnitude <= arriveRadius)
			{
				Release();
				Arrived = true;
				yield break;
			}

			Steer(delta);
			yield return null;

			if (Time.time < nextSample)
				continue;

			float moved = Vector2.Distance(position, lastSample);
			stuckFor = moved < StuckMinDistance ? stuckFor + StuckSampleInterval : 0;
			lastSample = position;
			nextSample = Time.time + StuckSampleInterval;

			if (stuckFor < StuckLimit)
				continue;

			Release();
			Failure = $"{position}에서 {stuckFor:0.0}초 동안 움직이지 못했다 " +
				$"(목표 {target}, 남은 거리 {delta.magnitude:0.00}, 막은 것: {DescribeBlockers(delta)})";
			yield break;
		}

		Release();
		Failure = $"{timeout:0.0}초 안에 {target}에 도달하지 못했다 (마지막 위치 {StoppedAt}, 남은 거리 {Vector2.Distance(StoppedAt, target):0.00})";
	}

	void Steer(Vector2 delta)
	{
		_keys.Clear();

		if (delta.x > AxisThreshold)
			_keys.Add(Key.D);
		else if (delta.x < -AxisThreshold)
			_keys.Add(Key.A);

		if (delta.y > AxisThreshold)
			_keys.Add(Key.W);
		else if (delta.y < -AxisThreshold)
			_keys.Add(Key.S);

		KeyboardState state = new KeyboardState();
		foreach (Key key in _keys)
			state.Set(key, true);

		Send(state);
	}
}
