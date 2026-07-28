using System;
using UnityEngine;

public class StageProgress : MonoBehaviour
{
	[SerializeField]
	int _requiredArtifacts = 4;

	int _collected;

	public Action<int, int> OnArtifactCollected;
	public Action OnAllArtifactsCollected;

	public int Collected { get { return _collected; } }
	public int Required { get { return _requiredArtifacts; } }
	public bool IsComplete { get { return _collected >= _requiredArtifacts; } }

	public void ResetProgress()
	{
		_collected = 0;
	}

	public void ReportCollected()
	{
		if (IsComplete)
			return;

		_collected++;

		if (OnArtifactCollected != null)
			OnArtifactCollected.Invoke(_collected, _requiredArtifacts);

		if (IsComplete && OnAllArtifactsCollected != null)
			OnAllArtifactsCollected.Invoke();
	}
}
