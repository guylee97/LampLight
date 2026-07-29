using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager
{
    AudioSource[] _audioSources = new AudioSource[(int)Define.Sound.MaxCount];
    Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();


    public void Init()
    {
        GameObject root = GameObject.Find("@Sound");
        if (root == null)
        {
            root = new GameObject { name = "@Sound" };

            if (Application.isPlaying)
                Object.DontDestroyOnLoad(root);
            else
                root.hideFlags = HideFlags.HideAndDontSave;
        }

        string[] soundNames = System.Enum.GetNames(typeof(Define.Sound));
        for (int i = 0; i < soundNames.Length - 1; i++)
        {
            if (_audioSources[i] != null)
                continue;

            Transform child = root.transform.Find(soundNames[i]);
            GameObject go = child != null ? child.gameObject : new GameObject { name = soundNames[i] };
            go.transform.parent = root.transform;
            _audioSources[i] = Util.GetOrAddComponent<AudioSource>(go);
        }

        _audioSources[(int)Define.Sound.Bgm].loop = true;
    }

    public void Clear()
    {
        foreach (AudioSource audioSource in _audioSources)
        {
            if (audioSource == null)
                continue;

            audioSource.clip = null;
            audioSource.Stop();
        }
        _audioClips.Clear();
    }

    public void Play(string path, Define.Sound type = Define.Sound.Effect, float pitch = 1.0f)
    {
        AudioClip audioClip = GetOrAddAudioClip(path, type);
        Play(audioClip, type, pitch);
    }

	public void Play(AudioClip audioClip, Define.Sound type = Define.Sound.Effect, float pitch = 1.0f)
	{
        if (audioClip == null)
            return;

		if (type == Define.Sound.Bgm)
		{
			AudioSource audioSource = _audioSources[(int)Define.Sound.Bgm];
			if (audioSource.isPlaying)
				audioSource.Stop();

			audioSource.pitch = pitch;
			audioSource.clip = audioClip;
			audioSource.Play();
		}
		else
		{
			AudioSource audioSource = _audioSources[(int)Define.Sound.Effect];
			audioSource.pitch = pitch;
			audioSource.PlayOneShot(audioClip);
		}
	}

	public void PlayAtPoint(string path, Vector3 position, float volume = 1.0f, float pitch = 1.0f)
	{
		AudioClip audioClip = GetOrAddAudioClip(path, Define.Sound.Effect);
		PlayAtPoint(audioClip, position, volume, pitch);
	}

	public void PlayAtPoint(AudioClip audioClip, Vector3 position, float volume = 1.0f, float pitch = 1.0f)
	{
		if (audioClip == null)
			return;

		if (Mathf.Approximately(pitch, 0))
			pitch = 1.0f;

		GameObject go = new GameObject { name = $"@Sound_{audioClip.name}" };
		go.transform.position = position;

		AudioSource audioSource = go.AddComponent<AudioSource>();
		audioSource.clip = audioClip;
		audioSource.volume = volume;
		audioSource.pitch = pitch;
		audioSource.spatialBlend = 1.0f;
		audioSource.Play();

		Object.Destroy(go, audioClip.length / Mathf.Abs(pitch));
	}

	AudioClip GetOrAddAudioClip(string path, Define.Sound type = Define.Sound.Effect)
    {
		if (path.Contains("Sounds/") == false)
			path = $"Sounds/{path}";

		AudioClip audioClip = null;

		if (type == Define.Sound.Bgm)
		{
			audioClip = Managers.Resource.Load<AudioClip>(path);
		}
		else
		{
			if (_audioClips.TryGetValue(path, out audioClip) == false)
			{
				audioClip = Managers.Resource.Load<AudioClip>(path);
				_audioClips.Add(path, audioClip);
			}
		}

		if (audioClip == null)
			Debug.Log($"AudioClip Missing ! {path}");

		return audioClip;
    }
}
