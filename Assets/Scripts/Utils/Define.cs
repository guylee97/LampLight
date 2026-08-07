using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    public enum WorldObject
    {
        Unknown,
        Player,
        Enemy,
    }

	public enum State
	{
		Die,
		Moving,
		Idle,
		Skill,
	}

	public enum EnemyState
	{
		Idle,
		Patrol,
		Chasing,
		Caught,
		Die,
		Searching,
		Petrified,
	}

	public enum Awareness
	{
		Unaware,
		Suspicious,
		Alerted,
	}

    public enum Layer
    {
        Enemy = 8,
        Player = 9,
        Block = 10,
    }

	public enum StageResult
	{
		None,
		Cleared,
		Caught,
	}

    public enum Scene
    {
        Unknown,
        Title,
        InGame,
    }

    public enum Sound
    {
        Bgm,
        Guide,
        Threat,
        Self,
        Ambient,
        UI,
        MaxCount,
    }

    public enum UIEvent
    {
        Click,
        Drag,
    }

    public enum MouseEvent
    {
        Press,
        PointerDown,
        PointerUp,
        Click,
    }

    public enum CameraMode
    {
        QuarterView,
    }
}
