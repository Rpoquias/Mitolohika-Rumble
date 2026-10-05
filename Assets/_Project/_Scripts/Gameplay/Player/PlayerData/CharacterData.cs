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

    [Header("Character Icon")]
    public Sprite characterIcon;

    [Header("Lobby Prefab")]
    public GameObject lobbyPrefab;

    [Header("Gameplay Prefab")]
    public GameObject gameplayPrefab;

    [Header("Signature Ability")]
    public string signatureAbility;

    [TextArea(2, 5)]
    public string abilityDescription;

    [Header("Ability Stats")]
    public float abilityCooldown;
    public float abilityRange;
    public float abilityDuration;
}