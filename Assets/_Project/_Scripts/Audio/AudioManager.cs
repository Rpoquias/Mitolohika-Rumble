using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Library")]
    [SerializeField] private AudioLibrary audioLibrary;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private string musicVolumeParameter = "MusicVolume";

    [Header("Music Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;

    [Header("Music Settings")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Min(0f)] private float crossfadeDuration = 1.5f;
[Header("SFX Mixer")]
[SerializeField] private string sfxVolumeParameter = "SFXVolume";

[Header("SFX Settings")]
[SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
[SerializeField, Min(1)] private int sfxPoolSize = 12;
[Header("3D SFX Settings")]
[SerializeField, Min(0.1f)] private float sfx3DMinDistance = 2f;
[SerializeField, Min(1f)] private float sfx3DMaxDistance = 20f;
[SerializeField] private AudioMixerGroup sfxMixerGroup;

private readonly List<AudioSource> sfxSources = new();
private int nextSFXSourceIndex;
    private AudioSource currentSource;
    private AudioSource previousSource;

    private Coroutine crossfadeCoroutine;
    private MusicID currentMusic = MusicID.None;

    public MusicID CurrentMusic => currentMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        ConfigureSource(musicSourceA);
        ConfigureSource(musicSourceB);

        currentSource = musicSourceA;
        previousSource = musicSourceB;

        SetMusicVolume(musicVolume);
        CreateSFXPool();
SetSFXVolume(sfxVolume);
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (audioLibrary == null)
        {
            Debug.LogError("[AUDIO] AudioLibrary is not assigned.", this);
            valid = false;
        }

        if (mainAudioMixer == null)
        {
            Debug.LogError("[AUDIO] MainAudioMixer is not assigned.", this);
            valid = false;
        }

        if (musicSourceA == null || musicSourceB == null)
        {
            Debug.LogError(
                "[AUDIO] Both music AudioSources must be assigned.",
                this
            );

            valid = false;
        }
        else if (musicSourceA == musicSourceB)
        {
            Debug.LogError(
                "[AUDIO] Music sources A and B must be different.",
                this
            );

            valid = false;
        }
        if (sfxMixerGroup == null)
{
    Debug.LogError(
        "[AUDIO] SFX Mixer Group is not assigned.",
        this
    );

    valid = false;
}

        return valid;
    }

    private void ConfigureSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;

        source.Stop();
        source.clip = null;
    }

    public void PlayMusic(MusicID musicID)
    {
        if (musicID == MusicID.None)
        {
            StopMusic();
            return;
        }

        if (!enabled || audioLibrary == null)
            return;

        if (!audioLibrary.TryGetMusic(musicID, out AudioClip clip))
        {
            Debug.LogWarning(
                $"[AUDIO] No music clip available for {musicID}.",
                this
            );

            return;
        }

        // Avoid restarting a track already playing or fading in.
        if (currentMusic == musicID &&
            currentSource.clip == clip &&
            currentSource.isPlaying)
        {
            return;
        }

        // Cancel the previous transition, preserving current source volumes.
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = null;
        }

        // Discard the older track. The current track keeps its volume.
        previousSource.Stop();
        previousSource.clip = null;
        previousSource.volume = 0f;

        // The free source becomes the incoming source.
        (currentSource, previousSource) =
            (previousSource, currentSource);

        currentSource.clip = clip;
        currentSource.volume = 0f;
        currentSource.Play();

        currentMusic = musicID;

        if (crossfadeDuration <= 0f)
        {
            currentSource.volume = 1f;

            previousSource.Stop();
            previousSource.clip = null;
            previousSource.volume = 0f;

            return;
        }

        crossfadeCoroutine = StartCoroutine(
            CrossfadeMusic(currentSource, previousSource)
        );

        Debug.Log($"[AUDIO] Transitioning to music: {musicID}", this);
    }

    private IEnumerator CrossfadeMusic(
        AudioSource incoming,
        AudioSource outgoing)
    {
        float incomingStart = incoming.volume;
        float outgoingStart = outgoing.volume;
        float elapsed = 0f;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / crossfadeDuration
            );

            incoming.volume = Mathf.Lerp(
                incomingStart,
                1f,
                progress
            );

            outgoing.volume = Mathf.Lerp(
                outgoingStart,
                0f,
                progress
            );

            yield return null;
        }

        incoming.volume = 1f;

        outgoing.Stop();
        outgoing.clip = null;
        outgoing.volume = 0f;

        crossfadeCoroutine = null;
    }


public void PlaySFXAtPosition(SFXID sfxID, Vector3 position)
{
    if (!enabled || audioLibrary == null)
        return;

    if (sfxID == SFXID.None)
        return;

    if (!audioLibrary.TryGetSFX(sfxID, out AudioClip clip))
    {
        Debug.LogWarning(
            $"[AUDIO] No SFX clip available for {sfxID}.",
            this
        );

        return;
    }

    if (sfxSources.Count == 0)
        return;

  AudioSource source = GetNextSFXSource();

source.transform.position = position;
source.spatialBlend = 1f;

source.minDistance = sfx3DMinDistance;
source.maxDistance = Mathf.Max(
    sfx3DMinDistance,
    sfx3DMaxDistance
);

source.PlayOneShot(clip);
}
private void CreateSFXPool()
{
    sfxSources.Clear();

    for (int i = 0; i < sfxPoolSize; i++)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
        source.outputAudioMixerGroup = sfxMixerGroup;

        sfxSources.Add(source);
    }
}    public void PlaySFX(SFXID sfxID)
{
    if (!enabled || audioLibrary == null)
        return;

    if (sfxID == SFXID.None)
        return;

    if (!audioLibrary.TryGetSFX(sfxID, out AudioClip clip))
    {
        Debug.LogWarning(
            $"[AUDIO] No SFX clip available for {sfxID}.",
            this
        );

        return;
    }

    if (sfxSources.Count == 0)
        return;

  AudioSource source = GetNextSFXSource();

source.transform.position = transform.position;
source.spatialBlend = 0f;

source.PlayOneShot(clip);
}
private AudioSource GetNextSFXSource()
{
    AudioSource source = sfxSources[nextSFXSourceIndex];

    nextSFXSourceIndex =
        (nextSFXSourceIndex + 1) % sfxSources.Count;

    return source;
}
public void SetSFXVolume(float volume)
{
    sfxVolume = Mathf.Clamp01(volume);

    if (mainAudioMixer == null)
        return;

    float decibels = sfxVolume > 0f
        ? Mathf.Log10(sfxVolume) * 20f
        : -80f;

    if (!mainAudioMixer.SetFloat(
            sfxVolumeParameter,
            decibels))
    {
        Debug.LogWarning(
            $"[AUDIO] Could not set mixer parameter " +
            $"'{sfxVolumeParameter}'. Check the exposed parameter.",
            this
        );
    }
}

    public void StopMusic()
    {
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = null;
        }

        StopSource(musicSourceA);
        StopSource(musicSourceB);

        currentMusic = MusicID.None;
    }

    private void StopSource(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.clip = null;
        source.volume = 0f;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);

        if (mainAudioMixer == null)
            return;

        float decibels = musicVolume > 0f
            ? Mathf.Log10(musicVolume) * 20f
            : -80f;

        if (!mainAudioMixer.SetFloat(
                musicVolumeParameter,
                decibels))
        {
            Debug.LogWarning(
                $"[AUDIO] Could not set mixer parameter " +
                $"'{musicVolumeParameter}'. Check the exposed parameter.",
                this
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}