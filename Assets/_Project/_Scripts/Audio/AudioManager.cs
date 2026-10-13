    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Audio;

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Library")]
        [SerializeField] private AudioLibrary audioLibrary;

      [Header("Audio Mixer")]
[SerializeField] private AudioMixer mainAudioMixer;

[SerializeField] private string masterVolumeParameter = "MasterVolume";
[SerializeField] private string musicVolumeParameter = "MusicVolume";
[SerializeField] private string sfxVolumeParameter = "SFXVolume";

[Header("Volume Settings")]
[SerializeField, Range(0f, 1f)]
private float masterVolume = 1f;

[SerializeField, Range(0f, 1f)]
private float musicVolume = 1f;

[SerializeField, Range(0f, 1f)]
private float sfxVolume = 1f;
        [Header("Audio Mixer Groups")]
[SerializeField] private AudioMixerGroup musicMixerGroup;
[SerializeField] private AudioMixerGroup sfxMixerGroup;

        [Header("Music Sources")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;


        [SerializeField, Min(0f)]
        private float crossfadeDuration = 1.5f;

        [SerializeField, Min(1)]
        private int sfxPoolSize = 12;

        [Header("3D SFX Settings")]
        [SerializeField, Min(0.1f)]
        private float sfx3DMinDistance = 2f;

        [SerializeField, Min(1f)]
        private float sfx3DMaxDistance = 20f;

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

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            ConfigureMusicSource(musicSourceA);
            ConfigureMusicSource(musicSourceB);

            currentSource = musicSourceA;
            previousSource = musicSourceB;

            CreateSFXPool();
            
        }

private IEnumerator Start()
{
    // Allow the AudioMixer to finish initializing.
    yield return null;

    masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
    musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
    sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

    SetMasterVolume(masterVolume);
    SetMusicVolume(musicVolume);
    SetSFXVolume(sfxVolume);
}
      private bool ValidateReferences()
        {
            bool valid = true;

            if (audioLibrary == null)
            {
                Debug.LogError(
                    "[AUDIO] AudioLibrary is not assigned.",
                    this
                );

                valid = false;
            }

            if (mainAudioMixer == null)
            {
                Debug.LogError(
                    "[AUDIO] MainAudioMixer is not assigned.",
                    this
                );

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

        private void ConfigureMusicSource(AudioSource source)
{
    source.playOnAwake = false;
    source.loop = true;
    source.spatialBlend = 0f;
    source.volume = 1f;

    source.outputAudioMixerGroup = musicMixerGroup;

    source.Stop();
    source.clip = null;
}

        // --------------------------------------------------
        // MUSIC
        // --------------------------------------------------

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

            if (currentMusic == musicID &&
                currentSource.clip == clip &&
                currentSource.isPlaying)
            {
                return;
            }

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
                crossfadeCoroutine = null;
            }

            previousSource.Stop();
            previousSource.clip = null;
            previousSource.volume = 0f;

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

            Debug.Log(
                $"[AUDIO] Transitioning to music: {musicID}",
                this
            );
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

        public void StopMusic()
        {
            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
                crossfadeCoroutine = null;
            }

            StopMusicSource(musicSourceA);
            StopMusicSource(musicSourceB);

            currentMusic = MusicID.None;
        }

        private void StopMusicSource(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        // --------------------------------------------------
        // SOUND EFFECTS
        // --------------------------------------------------

        public void PlaySFX(SFXID sfxID)
        {
            PlaySFXInternal(sfxID, Vector3.zero, false);
        }

        public void PlaySFXAtPosition(
            SFXID sfxID,
            Vector3 position)
        {
            PlaySFXInternal(sfxID, position, true);
        }

        private void PlaySFXInternal(
            SFXID sfxID,
            Vector3 position,
            bool spatial)
        {
            if (!enabled || audioLibrary == null)
                return;

            if (sfxID == SFXID.None)
                return;

            if (!audioLibrary.TryGetSFX(
                    sfxID,
                    out AudioClip clip))
            {
                Debug.LogWarning(
                    $"[AUDIO] No SFX clip available for {sfxID}.",
                    this
                );

                return;
            }

            if (sfxSources.Count == 0)
                return;

            AudioSource source = GetFreeSFXSource();

            source.transform.position = position;

            source.spatialBlend = spatial ? 1f : 0f;

            source.minDistance = sfx3DMinDistance;

            source.maxDistance = Mathf.Max(
                sfx3DMinDistance,
                sfx3DMaxDistance
            );

            source.rolloffMode = AudioRolloffMode.Linear;

            source.clip = clip;
            source.Play();
        }

        private void CreateSFXPool()
        {
            sfxSources.Clear();

            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sourceObject = new GameObject(
                    $"SFX Source {i}"
                );

                sourceObject.transform.SetParent(
                    transform,
                    false
                );

                AudioSource source =
                    sourceObject.AddComponent<AudioSource>();

                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.volume = 1f;
                source.outputAudioMixerGroup = sfxMixerGroup;

                source.minDistance = sfx3DMinDistance;

                source.maxDistance = Mathf.Max(
                    sfx3DMinDistance,
                    sfx3DMaxDistance
                );

                source.rolloffMode = AudioRolloffMode.Linear;

                sfxSources.Add(source);
            }

            nextSFXSourceIndex = 0;
        }

public AudioSource CreateLoopingSFX(
    SFXID sfxID,
    Transform target)
{
    if (!enabled ||
        audioLibrary == null ||
        target == null)
    {
        return null;
    }

    if (!audioLibrary.TryGetSFX(
            sfxID,
            out AudioClip clip))
    {
        Debug.LogWarning(
            $"[AUDIO] No SFX clip available for {sfxID}.",
            this
        );

        return null;
    }

    GameObject sourceObject = new GameObject(
        $"{sfxID} Audio"
    );

    sourceObject.transform.SetParent(target, false);

    AudioSource source =
        sourceObject.AddComponent<AudioSource>();

    source.playOnAwake = false;
    source.loop = true;
    source.clip = clip;
    source.volume = 1f;

    source.spatialBlend = 1f;
    source.minDistance = sfx3DMinDistance;
    source.maxDistance = Mathf.Max(
        sfx3DMinDistance,
        sfx3DMaxDistance
    );

    source.rolloffMode = AudioRolloffMode.Linear;
    source.outputAudioMixerGroup = sfxMixerGroup;

    return source;
}
        private AudioSource GetFreeSFXSource()
        {
            for (int i = 0; i < sfxSources.Count; i++)
            {
                int index =
                    (nextSFXSourceIndex + i) % sfxSources.Count;

                AudioSource source = sfxSources[index];

                if (source.isPlaying)
                    continue;

                nextSFXSourceIndex =
                    (index + 1) % sfxSources.Count;

                return source;
            }

            // Every source is busy. Reuse the next source.
            AudioSource stolenSource =
                sfxSources[nextSFXSourceIndex];

            stolenSource.Stop();

            nextSFXSourceIndex =
                (nextSFXSourceIndex + 1) % sfxSources.Count;

            return stolenSource;
        }

        // --------------------------------------------------
        // VOLUME
        // --------------------------------------------------
// --------------------------------------------------
// VOLUME
// --------------------------------------------------

// --------------------------------------------------
// VOLUME
// --------------------------------------------------

public void SetMasterVolume(float volume)
{
    masterVolume = Mathf.Clamp01(volume);

    SetMixerVolume(
        masterVolumeParameter,
        masterVolume
    );

    PlayerPrefs.SetFloat("MasterVolume", masterVolume);
}

public void SetMusicVolume(float volume)
{
    musicVolume = Mathf.Clamp01(volume);

    SetMixerVolume(
        musicVolumeParameter,
        musicVolume
    );

    PlayerPrefs.SetFloat("MusicVolume", musicVolume);
}

public void SetSFXVolume(float volume)
{
    sfxVolume = Mathf.Clamp01(volume);

    SetMixerVolume(
        sfxVolumeParameter,
        sfxVolume
    );

    PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
}

public void SaveVolumeSettings()
{
    PlayerPrefs.Save();
}

private void SetMixerVolume(
    string parameter,
    float volume)
{
    if (mainAudioMixer == null)
        return;

    float decibels = volume > 0f
        ? Mathf.Log10(volume) * 20f
        : -80f;

    if (!mainAudioMixer.SetFloat(parameter, decibels))
    {
        Debug.LogWarning(
            $"[AUDIO] Could not set mixer parameter '{parameter}'.",
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