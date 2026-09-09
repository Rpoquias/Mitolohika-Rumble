using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Mitolohika Rumble/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Character Identity")]
    public CharacterID characterID;
    public string characterName;

    [Header("Character Prefab")]
    public GameObject characterPrefab;

    [Header("Signature Ability")]
    public string signatureAbility;
}