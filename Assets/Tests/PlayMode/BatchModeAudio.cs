using UnityEngine;

public static class BatchModeAudio
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Mute()
	{
		if (Application.isBatchMode == false)
			return;

		AudioListener.volume = 0.0f;
		Debug.Log("BatchModeAudio: 배치 모드라 소리를 껐다");
	}
}
