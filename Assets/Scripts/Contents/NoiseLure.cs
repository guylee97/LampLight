using System.Collections.Generic;
using UnityEngine;

public class NoiseLure : MonoBehaviour
{
	static readonly List<NoiseLure> s_active = new List<NoiseLure>();

	float _radius;
	float _expiry;

	public float Radius { get { return _radius; } }

	public static IReadOnlyList<NoiseLure> Active { get { return s_active; } }

	public static NoiseLure Spawn(Vector3 position, float radius, float duration)
	{
		GameObject go = new GameObject("@NoiseLure");
		go.transform.position = position;

		NoiseLure lure = go.AddComponent<NoiseLure>();
		lure._radius = radius;
		lure._expiry = Time.time + duration;
		return lure;
	}

	public static bool TryHear(Vector3 listener, out Vector3 point)
	{
		point = Vector3.zero;
		float best = float.MaxValue;
		bool found = false;

		foreach (NoiseLure lure in s_active)
		{
			if (lure == null)
				continue;

			float distance = Vector2.Distance(listener, lure.transform.position);
			if (distance > lure._radius || distance >= best)
				continue;

			best = distance;
			point = lure.transform.position;
			found = true;
		}

		return found;
	}

	public static void ClearAll()
	{
		for (int i = s_active.Count - 1; i >= 0; i--)
		{
			if (s_active[i] != null)
				Destroy(s_active[i].gameObject);
		}

		s_active.Clear();
	}

	void OnEnable()
	{
		if (s_active.Contains(this) == false)
			s_active.Add(this);
	}

	void OnDisable()
	{
		s_active.Remove(this);
	}

	void Update()
	{
		if (Time.time >= _expiry)
			Destroy(gameObject);
	}
}
