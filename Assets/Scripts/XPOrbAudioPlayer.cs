using System.Collections.Generic;
using UnityEngine;

public sealed class XPOrbAudioPlayer : MonoBehaviour
{
    private const string ClipResourcePath = "SFX/XPOrb";
    private const int InitialPoolSize = 8;
    private const float SingleVoiceVolume = 0.7f;
    private const float MinimumVoiceVolume = 0.18f;

    private static XPOrbAudioPlayer instance;

    private readonly List<AudioSource> sources = new();
    private AudioClip clip;
    private int activeVoiceCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        EnsureInstance();
    }

    public static void Play()
    {
        XPOrbAudioPlayer player = EnsureInstance();
        if (player == null || player.clip == null)
        {
            return;
        }

        player.PlayNextVoice();
    }

    private static XPOrbAudioPlayer EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject playerObject = new(nameof(XPOrbAudioPlayer));
        DontDestroyOnLoad(playerObject);
        instance = playerObject.AddComponent<XPOrbAudioPlayer>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        clip = Resources.Load<AudioClip>(ClipResourcePath);
        if (clip == null)
        {
            Debug.LogError($"XPOrbAudioPlayer could not load Resources/{ClipResourcePath}.mp3");
            return;
        }

        clip.LoadAudioData();
        for (int i = 0; i < InitialPoolSize; i++)
        {
            CreateSource();
        }
    }

    private void Update()
    {
        int playingCount = CountPlayingSources();
        if (playingCount != activeVoiceCount)
        {
            activeVoiceCount = playingCount;
            ApplyOverlapVolume();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void PlayNextVoice()
    {
        AudioSource source = GetAvailableSource();
        source.clip = clip;
        source.pitch = 1f;
        source.time = 0f;
        source.Play();

        activeVoiceCount = CountPlayingSources();
        ApplyOverlapVolume();
    }

    private AudioSource GetAvailableSource()
    {
        foreach (AudioSource source in sources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        return CreateSource();
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.priority = 64;
        sources.Add(source);
        return source;
    }

    private int CountPlayingSources()
    {
        int count = 0;
        foreach (AudioSource source in sources)
        {
            if (source.isPlaying)
            {
                count++;
            }
        }

        return count;
    }

    private void ApplyOverlapVolume()
    {
        float volume = activeVoiceCount > 0
            ? Mathf.Max(MinimumVoiceVolume, SingleVoiceVolume / Mathf.Sqrt(activeVoiceCount))
            : SingleVoiceVolume;

        foreach (AudioSource source in sources)
        {
            if (source.isPlaying)
            {
                source.volume = volume;
            }
        }
    }
}
