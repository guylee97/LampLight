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
		Stunned,
		Caught,
		Die,
	}

    public enum Layer
    {
        Enemy = 8,
        Ground = 9,
        Block = 10,
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
        Effect,
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
