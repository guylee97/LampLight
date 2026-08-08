using System.Collections.Generic;
using UnityEngine;

/// 사물이 말을 건다. 다가가면 발동하고 게임은 완전히 멈춘다.
///
/// 콜라이더를 붙이지 않고 거리로만 본다. 트리거는 전각당 열 개 남짓이라
/// 매 프레임 훑어도 비용이 없고, 액터가 걸려 넘어질 물체를 늘리지 않는다.
public class DialogueDirector : MonoBehaviour
{
	struct Spot
	{
		public DialogueTrigger Trigger;
		public Vector3 Position;
	}

	readonly List<Spot> _spots = new List<Spot>();

	StageProgress _progress;
	Transform _player;
	int _level;

	public int Count { get { return _spots.Count; } }

	public static DialogueDirector Ensure()
	{
		DialogueDirector found = FindFirstObjectByType<DialogueDirector>();
		if (found != null)
			return found;

		GameObject go = new GameObject("@Dialogue Director");
		return go.AddComponent<DialogueDirector>();
	}

	public void Build(int level, StageProgress progress, Transform player, MapDecoPlacer deco)
	{
		_level = level;
		_progress = progress;
		_player = player;
		_spots.Clear();

		DialogueTrigger[] triggers = DialogueTable.Book.triggers;
		if (triggers == null || deco == null)
			return;

		foreach (DialogueTrigger trigger in triggers)
		{
			if (trigger.level != level || trigger.lines == null || trigger.lines.Length == 0)
				continue;

			if (trigger.once && DialogueMemory.HasSeen(trigger.id))
				continue;

			Vector3 where;
			if (deco.TryClosestToRoute(trigger.anchor, out where) == false)
			{
				Debug.LogWarning($"DialogueDirector: L{level} 에 '{trigger.anchor}' 가 없어 {trigger.id} 를 걸지 못했다");
				continue;
			}

			_spots.Add(new Spot { Trigger = trigger, Position = where });
		}
	}

	void Update()
	{
		if (_spots.Count == 0 || _player == null)
			return;

		if (Managers.Game.IsPlaying == false || UI_Dialogue.IsShowing)
			return;

		int collected = _progress != null ? _progress.Collected : 0;
		int required = _progress != null ? _progress.Required : 0;

		for (int i = 0; i < _spots.Count; i++)
		{
			Spot spot = _spots[i];

			if (spot.Trigger.Allows(collected, required) == false)
				continue;

			float radius = spot.Trigger.radius;
			if ((spot.Position - _player.position).sqrMagnitude > radius * radius)
				continue;

			// 대사 중에 다른 트리거가 겹치면 큐에 쌓지 않고 버린다. 한 목소리만 남는다.
			UI_Dialogue.Say(DialogueTable.Speaker(spot.Trigger.voice), spot.Trigger.lines);

			if (spot.Trigger.once)
				DialogueMemory.MarkSeen(spot.Trigger.id);

			_spots.RemoveAt(i);
			return;
		}
	}
}
