using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseController : MonoBehaviour
{
	[SerializeField]
	protected Define.State _state = Define.State.Idle;

	public Define.WorldObject WorldObjectType { get; protected set; } = Define.WorldObject.Unknown;

	Animator _animator;
	bool _animatorSearched;

	protected Animator Animator
	{
		get
		{
			if (_animatorSearched == false)
			{
				_animator = GetComponent<Animator>();
				_animatorSearched = true;
			}

			return _animator;
		}
	}

	public virtual Define.State State
	{
		get { return _state; }
		set
		{
			_state = value;

			Animator anim = Animator;
			if (anim == null || anim.runtimeAnimatorController == null)
				return;

			switch (_state)
			{
				case Define.State.Die:
					break;
				case Define.State.Idle:
					anim.CrossFade("WAIT", 0.1f);
					break;
				case Define.State.Moving:
					anim.CrossFade("RUN", 0.1f);
					break;
				case Define.State.Skill:
					anim.CrossFade("Skill", 0.1f, -1, 0);
					break;
			}
		}
	}

	protected virtual void Start()
	{
		Init();
	}

	protected virtual void Update()
	{
		switch (State)
		{
			case Define.State.Die:
				UpdateDie();
				break;
			case Define.State.Moving:
				UpdateMoving();
				break;
			case Define.State.Idle:
				UpdateIdle();
				break;
			case Define.State.Skill:
				UpdateSkill();
				break;
		}
	}

	public abstract void Init();

	protected virtual void UpdateDie() { }
	protected virtual void UpdateMoving() { }
	protected virtual void UpdateIdle() { }
	protected virtual void UpdateSkill() { }
}
