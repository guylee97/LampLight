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

	public enum Direction8
	{
		E,
		NE,
		N,
		NW,
		W,
		SW,
		S,
		SE,
	}

    public enum Layer
    {
        Enemy = 8,
        Player = 9,
        Block = 10,
    }

	public enum Alert
	{
		Patrol,
		Suspicious,
		Chase,
		Search,
		Blinded,
		Caught,
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
