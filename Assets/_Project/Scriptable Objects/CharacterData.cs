using UnityEngine;

[CreateAssetMenu(
    fileName = "NewCharacterData",
    menuName = "Mitolohika Rumble/Character Data"
)]
public class CharacterData : ScriptableObject
{
    [Header("Character Identity")]
    public CharacterID characterID;
    public string characterName;

    [Header("Lobby Prefab")]
    public GameObject lobbyPrefab;

    [Header("Gameplay Prefab")]
    public GameObject gameplayPrefab;

    [Header("Signature Ability")]
    public string signatureAbility;
}