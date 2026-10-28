using System;
using UnityEngine;

public class KapiNetworkPlayerState : MonoBehaviour
{
    [SerializeField] public CharacterID SelectedCharacter;
    [SerializeField] public bool IsReady;
    public Action<KapiNetworkPlayerState> OnUIStateChanged;
    public bool IsInitialized { get; private set; }
    public bool IsValid => IsInitialized && isActiveAndEnabled;

    private void Awake()
    {
        IsInitialized = true;
    }

    public void RequestCharacterChange(CharacterID characterID)
    {
        if (SelectedCharacter == characterID)
            return;
        SelectedCharacter = characterID;
        OnUIStateChanged?.Invoke(this);
    }

    public void ToggleReady()
    {
        IsReady = !IsReady;
        OnUIStateChanged?.Invoke(this);
    }

    private void OnDestroy() => OnUIStateChanged = null;
}