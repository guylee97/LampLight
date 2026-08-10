using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

public class SoundManager
{
	const int MaxSpatialVoices = 12;
	const float OcclusionUpdateInterval = 0.1f;
	const float ReferenceDistance = 1.0f;
	const float MaximumDistance = 12.0f;
	const float RunningDuckDb = -7.0f;
	const float DuckAttackSeconds = 0.03f;
	const float DuckReleaseSeconds = 0.25f;

	readonly AudioSource[] _busSources = new AudioSource[(int)Define.Sound.MaxCount];
	readonly AudioMixerGroup[] _mixerGroups = new AudioMixerGroup[(int)Define.Sound.MaxCount];
	readonly bool[] _mixerControlsBus = new bool[(int)Define.Sound.MaxCount];
	readonly Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();
	readonly List<SpatialVoice> _spatialVoices = new List<SpatialVoice>();
	readonly List<RoutedSource> _routedSources = new List<RoutedSource>();
	readonly float[] _currentBusDb = new float[(int)Define.Sound.MaxCount];

	AudioMixer _mixer;
	Transform _root;
	bool _isListening;
	bool _isRunning;

	public bool IsListening { get { return _isListening; } }

	public void Init()
	{
		GameObject rootObject = GameObject.Find("@Sound");
		if (rootObject == null)
		{
			rootObject = new GameObject { name = "@Sound" };

			if (Application.isPlaying)
				Object.DontDestroyOnLoad(rootObject);
			else
				rootObject.hideFlags = HideFlags.HideAndDontSave;
		}

		_root = rootObject.transform;
		LoadMixerRouting();

		for (int i = 0; i < (int)Define.Sound.MaxCount; i++)
		{
			Define.Sound bus = (Define.Sound)i;
			Transform child = _root.Find(bus.ToString());
			GameObject go = child != null ? child.gameObject : new GameObject { name = bus.ToString() };
			go.transform.SetParent(_root);

			AudioSource source = Util.GetOrAddComponent<AudioSource>(go);
			source.playOnAwake = false;
			source.outputAudioMixerGroup = _mixerGroups[i];
			_busSources[i] = source;
		}

		_busSources[(int)Define.Sound.Ambient].loop = true;
		ApplyBusVolumesImmediate();
	}

	public void OnUpdate()
	{
		UpdateBusVolumes();
		UpdateSpatialVoices();
	}

	public void SetListening(bool listening)
	{
		_isListening = listening;
	}

	public void SetRunning(bool running)
	{
		_isRunning = running;
	}

	public void Clear()
	{
		foreach (AudioSource source in _busSources)
		{
			if (source == null)
				continue;

			source.clip = null;
			source.Stop();
		}

		for (int i = _spatialVoices.Count - 1; i >= 0; i--)
		{
			if (_spatialVoices[i].Source != null)
				Object.Destroy(_spatialVoices[i].Source.gameObject);
		}

		_spatialVoices.Clear();
		_routedSources.Clear();
		_audioClips.Clear();
		_isListening = false;
		_isRunning = false;
	}

	public void PlayOptional(
		string path,
		Define.Sound bus,
		float pitch = 1.0f,
		float volume = 1.0f,
		bool loop = false)
	{
		AudioClip clip = GetOptionalAudioClip(path);
		if (clip == null)
			return;

		Play(clip, bus, pitch, volume, loop);
	}

	public void PlayOptional(
		string path,
		string fallbackPath,
		Define.Sound bus,
		float pitch = 1.0f,
		float volume = 1.0f,
		bool loop = false)
	{
		AudioClip clip = GetOptionalAudioClip(path);
		if (clip == null)
			clip = GetOptionalAudioClip(fallbackPath);

		Play(clip, bus, pitch, volume, loop);
	}

	public void Play(
		AudioClip clip,
		Define.Sound bus,
		float pitch = 1.0f,
		float volume = 1.0f,
		bool loop = false)
	{
		if (clip == null)
			return;

		AudioSource source = _busSources[(int)bus];
		source.outputAudioMixerGroup = _mixerGroups[(int)bus];
		source.pitch = Mathf.Approximately(pitch, 0) ? 1.0f : pitch;
		source.volume = volume * GetSourceBusGain(bus);

		if (loop)
		{
			source.Stop();
			source.clip = clip;
			source.loop = true;
			source.Play();
		}
		else
		{
			source.PlayOneShot(clip);
		}
	}

	public void PlayAtPoint(
		AudioClip clip,
		Vector3 position,
		Define.Sound bus,
		float volume = 1.0f,
		float pitch = 1.0f,
		float maxDistance = -1.0f)
	{
		if (clip == null)
			return;

		RemoveFinishedVoices();
		if (_spatialVoices.Count >= MaxSpatialVoices)
			StopFarthestVoice(position);

		GameObject go = new GameObject { name = $"@Sound_{bus}_{clip.name}" };
		go.transform.position = position;
		go.transform.SetParent(_root);

		AudioSource source = go.AddComponent<AudioSource>();
		source.clip = clip;
		source.outputAudioMixerGroup = _mixerGroups[(int)bus];
		source.volume = volume * GetSourceBusGain(bus);
		source.pitch = Mathf.Approximately(pitch, 0) ? 1.0f : pitch;
		source.spatialBlend = 1.0f;
		source.dopplerLevel = 0;
		source.rolloffMode = AudioRolloffMode.Logarithmic;
		source.minDistance = ReferenceDistance;
		source.maxDistance = maxDistance > 0.0f ? maxDistance : MaximumDistance;
		source.playOnAwake = false;

		AudioLowPassFilter lowPass = go.AddComponent<AudioLowPassFilter>();
		lowPass.cutoffFrequency = 18000;

		_spatialVoices.Add(new SpatialVoice(source, lowPass, bus, volume));
		source.Play();

		if (Application.isPlaying)
			Object.Destroy(go, clip.length / Mathf.Abs(source.pitch) + 0.1f);
		else
			Object.DestroyImmediate(go);
	}

	public void PlayAtPointOptional(
		string path,
		Vector3 position,
		Define.Sound bus,
		float volume = 1.0f,
		float pitch = 1.0f)
	{
		AudioClip clip = GetOptionalAudioClip(path);
		if (clip == null)
			return;

		PlayAtPoint(clip, position, bus, volume, pitch);
	}

	public void PlayAtPointOptional(
		string path,
		string fallbackPath,
		Vector3 position,
		Define.Sound bus,
		float volume = 1.0f,
		float pitch = 1.0f)
	{
		AudioClip clip = GetOptionalAudioClip(path);
		if (clip == null)
			clip = GetOptionalAudioClip(fallbackPath);

		if (clip == null)
			return;

		PlayAtPoint(clip, position, bus, volume, pitch);
	}

	public void ConfigureSource(AudioSource source, Define.Sound bus, bool spatial)
	{
		if (source == null)
			return;

		source.outputAudioMixerGroup = _mixerGroups[(int)bus];
		source.spatialBlend = spatial ? 1.0f : 0.0f;
		source.dopplerLevel = 0;
		source.minDistance = ReferenceDistance;
		source.maxDistance = MaximumDistance;
		source.rolloffMode = AudioRolloffMode.Logarithmic;

		for (int i = 0; i < _routedSources.Count; i++)
		{
			if (_routedSources[i].Source == source)
				return;
		}

		AudioLowPassFilter lowPass = null;
		if (spatial)
			lowPass = Util.GetOrAddComponent<AudioLowPassFilter>(source.gameObject);

		_routedSources.Add(new RoutedSource(source, lowPass, bus, source.volume));
	}

	void LoadMixerRouting()
	{
		_mixer = Resources.Load<AudioMixer>("Audio/MainAudioMixer");
		if (_mixer == null)
			return;

		for (int i = 0; i < (int)Define.Sound.MaxCount; i++)
		{
			Define.Sound bus = (Define.Sound)i;
			AudioMixerGroup[] matches = _mixer.FindMatchingGroups(bus.ToString());
			if (matches.Length > 0)
				_mixerGroups[i] = matches[0];
		}
	}

	void UpdateBusVolumes()
	{
		for (int i = 0; i < (int)Define.Sound.MaxCount; i++)
		{
			Define.Sound bus = (Define.Sound)i;
			float target = GetTargetBusDb(bus);
			bool lowering = target < _currentBusDb[i];
			float duration = lowering ? DuckAttackSeconds : DuckReleaseSeconds;
			float speed = Mathf.Abs(target - _currentBusDb[i]) / Mathf.Max(duration, 0.001f);
			_currentBusDb[i] = Mathf.MoveTowards(_currentBusDb[i], target, speed * Time.unscaledDeltaTime);

			if (_mixer != null)
				_mixerControlsBus[i] = _mixer.SetFloat(bus + "Volume", _currentBusDb[i]);

			if (_busSources[i] != null)
				_busSources[i].volume = GetSourceBusGain(bus);
		}
	}

	void ApplyBusVolumesImmediate()
	{
		for (int i = 0; i < (int)Define.Sound.MaxCount; i++)
		{
			_currentBusDb[i] = GetTargetBusDb((Define.Sound)i);
			if (_busSources[i] != null)
				_busSources[i].volume = GetSourceBusGain((Define.Sound)i);
		}
	}

	float GetTargetBusDb(Define.Sound bus)
	{
		float db = 0;

		switch (bus)
		{
			case Define.Sound.Guide:
				db = -3;
				break;
			case Define.Sound.Threat:
				db = 0;
				break;
			case Define.Sound.Self:
				db = -1;
				break;
			case Define.Sound.Ambient:
				db = -24;
				break;
			case Define.Sound.UI:
				db = -6;
				break;
		}

		if (_isListening)
		{
			switch (bus)
			{
				case Define.Sound.Guide:
					db = 0;
					break;
				case Define.Sound.Threat:
					db = 0;
					break;
				case Define.Sound.Self:
					db = -12;
					break;
				case Define.Sound.Ambient:
					db = -30;
					break;
				case Define.Sound.UI:
					db = -9;
					break;
			}
		}

		if (_isRunning && (bus == Define.Sound.Guide || bus == Define.Sound.Threat))
			db += RunningDuckDb;

		return db;
	}

	void UpdateSpatialVoices()
	{
		RemoveFinishedVoices();

		Transform listener = GetListenerTransform();
		for (int i = _routedSources.Count - 1; i >= 0; i--)
		{
			RoutedSource routed = _routedSources[i];
			if (routed.Source == null)
			{
				_routedSources.RemoveAt(i);
				continue;
			}

			routed.Source.volume = routed.BaseVolume * GetSourceBusGain(routed.Bus);
			UpdateOcclusion(routed.Source, routed.LowPass, routed, listener);
		}

		for (int i = 0; i < _spatialVoices.Count; i++)
		{
			SpatialVoice voice = _spatialVoices[i];
			voice.Source.volume = voice.BaseVolume * GetSourceBusGain(voice.Bus);
			UpdateOcclusion(voice.Source, voice.LowPass, voice, listener);
		}
	}

	void UpdateOcclusion(
		AudioSource source,
		AudioLowPassFilter lowPass,
		OcclusionState state,
		Transform listener)
	{
		if (lowPass == null || listener == null || Time.unscaledTime < state.NextOcclusionUpdate)
			return;

		state.NextOcclusionUpdate = Time.unscaledTime + OcclusionUpdateInterval;
		int wallCount = CountWalls(listener.position, source.transform.position);
		lowPass.cutoffFrequency = Mathf.Max(350, 18000 * Mathf.Pow(0.45f, wallCount));
	}

	int CountWalls(Vector3 listenerPosition, Vector3 soundPosition)
	{
		Vector2 direction = soundPosition - listenerPosition;
		float distance = direction.magnitude;
		if (distance <= 0.01f)
			return 0;

		RaycastHit2D[] hits = Physics2D.RaycastAll(
			listenerPosition,
			direction.normalized,
			distance,
			1 << (int)Define.Layer.Block
		);

		return Mathf.Min(hits.Length, 8);
	}

	void RemoveFinishedVoices()
	{
		for (int i = _spatialVoices.Count - 1; i >= 0; i--)
		{
			AudioSource source = _spatialVoices[i].Source;
			if (source == null || source.isPlaying == false)
				_spatialVoices.RemoveAt(i);
		}
	}

	void StopFarthestVoice(Vector3 fallbackListenerPosition)
	{
		Transform listener = GetListenerTransform();
		Vector3 listenerPosition = listener == null ? fallbackListenerPosition : listener.position;
		int farthestIndex = -1;
		float farthestSqr = -1;

		for (int i = 0; i < _spatialVoices.Count; i++)
		{
			float sqr = (_spatialVoices[i].Source.transform.position - listenerPosition).sqrMagnitude;
			if (sqr <= farthestSqr)
				continue;

			farthestSqr = sqr;
			farthestIndex = i;
		}

		if (farthestIndex < 0)
			return;

		Object.Destroy(_spatialVoices[farthestIndex].Source.gameObject);
		_spatialVoices.RemoveAt(farthestIndex);
	}

	Transform GetListenerTransform()
	{
		AudioListener listener = Object.FindFirstObjectByType<AudioListener>();
		return listener == null ? null : listener.transform;
	}

	AudioClip GetOptionalAudioClip(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return null;

		if (_audioClips.TryGetValue(path, out AudioClip cached))
			return cached;

		string normalized = path.Replace("\\", "/");
		if (normalized.EndsWith(".wav") || normalized.EndsWith(".mp3") || normalized.EndsWith(".ogg"))
			normalized = normalized.Substring(0, normalized.LastIndexOf('.'));

		AudioClip clip = Resources.Load<AudioClip>(normalized);
		if (clip == null && normalized.StartsWith("Audio/") == false)
			clip = Resources.Load<AudioClip>("Audio/" + normalized);

		_audioClips[path] = clip;
		return clip;
	}

	static float DbToLinear(float db)
	{
		return Mathf.Pow(10.0f, db / 20.0f);
	}

	float GetSourceBusGain(Define.Sound bus)
	{
		int index = (int)bus;
		return _mixerControlsBus[index] ? 1.0f : DbToLinear(_currentBusDb[index]);
	}

	abstract class OcclusionState
	{
		public float NextOcclusionUpdate;
	}

	sealed class SpatialVoice : OcclusionState
	{
		public readonly AudioSource Source;
		public readonly AudioLowPassFilter LowPass;
		public readonly Define.Sound Bus;
		public readonly float BaseVolume;

		public SpatialVoice(
			AudioSource source,
			AudioLowPassFilter lowPass,
			Define.Sound bus,
			float baseVolume)
		{
			Source = source;
			LowPass = lowPass;
			Bus = bus;
			BaseVolume = baseVolume;
		}
	}

	sealed class RoutedSource : OcclusionState
	{
		public readonly AudioSource Source;
		public readonly AudioLowPassFilter LowPass;
		public readonly Define.Sound Bus;
		public readonly float BaseVolume;

		public RoutedSource(
			AudioSource source,
			AudioLowPassFilter lowPass,
			Define.Sound bus,
			float baseVolume)
		{
			Source = source;
			LowPass = lowPass;
			Bus = bus;
			BaseVolume = baseVolume;
		}
	}
}
