using System;
using UnityEngine;

public class StageProgress : MonoBehaviour
{
	[SerializeField]
	int _requiredArtifacts = 4;

	int _collected;
	float _weighted;

	public Action<int, int> OnArtifactCollected;
	public Action OnAllArtifactsCollected;

	public int Collected { get { return _collected; } }
	public float WeightedValue { get { return _weighted; } }
	public int Required { get { return _requiredArtifacts; } }
	public bool IsComplete { get { return _collected >= _requiredArtifacts; } }

	public void ResetProgress()
	{
		_collected = 0;
		_weighted = 0.0f;
	}

	public void SetRequired(int required)
	{
		_requiredArtifacts = required < 0 ? 0 : required;
	}

	public void ReportCollected()
	{
		ReportCollected(1.0f);
	}

	public void ReportCollected(float weight)
	{
		bool wasComplete = IsComplete;
		_collected++;
		_weighted += weight;

		if (OnArtifactCollected != null)
			OnArtifactCollected.Invoke(_collected, _requiredArtifacts);

		if (wasComplete == false && IsComplete && OnAllArtifactsCollected != null)
			OnAllArtifactsCollected.Invoke();
	}
}
