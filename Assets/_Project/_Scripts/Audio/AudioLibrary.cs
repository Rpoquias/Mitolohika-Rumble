using System;
using System.Collections.Generic;
using UnityEngine;

public enum MusicID
{
    None,
    MainMenu,
    Lobby,
    RoundReveal,
    Gameplay,
    Celebration
}

public enum SFXID
{
    None,

    // UI
    ButtonClick,
    ButtonConfirm,
    PlayerReady,

    // Match
    Countdown,
    RoundStart,
    RoundEnd,
    Victory,

    // Player
    PlayerMove,
    PlayerJump,
    PlayerLand,
    PlayerBump,
    PlayerHit,
    PlayerEliminated,

    // Character Abilities
    TikbalangKick,
    KapreSmoke,
    WhiteLadyInvisibility,
    AswangStun,
    NunoProjectile,
    SirenaDive,
    SirenaEmerge,

    // Game Mode SFX
    ArenaTileDestroyed,

    RelicPickup,
    RelicDrop,

    BandilaPickup,
    BandilaDrop,
    BandilaStored
}

[CreateAssetMenu(
    fileName = "AudioLibrary",
    menuName = "Mitolohika Rumble/Audio Library"
)]
public class AudioLibrary : ScriptableObject
{
    [Serializable]
    public struct MusicEntry
    {
        public MusicID id;
        public AudioClip clip;
    }

    [Serializable]
    public struct SFXEntry
    {
        public SFXID id;
        public AudioClip clip;
    }

    [Header("Music")]
    [SerializeField] private List<MusicEntry> musicEntries = new();

    [Header("UI SFX")]
    [SerializeField] private List<SFXEntry> uiSFX = new();

    [Header("Match SFX")]
    [SerializeField] private List<SFXEntry> matchSFX = new();

    [Header("Player SFX")]
    [SerializeField] private List<SFXEntry> playerSFX = new();

    [Header("Character Ability SFX")]
    [SerializeField] private List<SFXEntry> abilitySFX = new();

    [Header("Game Mode SFX")]
    [SerializeField] private List<SFXEntry> gameModeSFX = new();

    private Dictionary<MusicID, AudioClip> musicLookup;
    private Dictionary<SFXID, AudioClip> sfxLookup;

    private void OnEnable()
    {
        BuildLookups();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BuildLookups();
    }
#endif

    private void BuildLookups()
    {
        musicLookup = new Dictionary<MusicID, AudioClip>();
        sfxLookup = new Dictionary<SFXID, AudioClip>();

        foreach (MusicEntry entry in musicEntries)
        {
            if (entry.id == MusicID.None)
                continue;

            AddClip(
                musicLookup,
                entry.id,
                entry.clip
            );
        }

        AddSFXEntries(uiSFX);
        AddSFXEntries(matchSFX);
        AddSFXEntries(playerSFX);
        AddSFXEntries(abilitySFX);
        AddSFXEntries(gameModeSFX);
    }

    private void AddSFXEntries(List<SFXEntry> entries)
    {
        foreach (SFXEntry entry in entries)
        {
            if (entry.id == SFXID.None)
                continue;

            AddClip(
                sfxLookup,
                entry.id,
                entry.clip
            );
        }
    }

    private void AddClip<T>(
        Dictionary<T, AudioClip> lookup,
        T id,
        AudioClip clip)
    {
        // Skip duplicate IDs and entries without a clip.
        if (lookup.ContainsKey(id) || clip == null)
            return;

        lookup.Add(id, clip);
    }

    public bool TryGetMusic(
        MusicID id,
        out AudioClip clip)
    {
        if (musicLookup == null)
            BuildLookups();

        return musicLookup.TryGetValue(id, out clip);
    }

    public bool TryGetSFX(
        SFXID id,
        out AudioClip clip)
    {
        if (sfxLookup == null)
            BuildLookups();

        return sfxLookup.TryGetValue(id, out clip);
    }
}