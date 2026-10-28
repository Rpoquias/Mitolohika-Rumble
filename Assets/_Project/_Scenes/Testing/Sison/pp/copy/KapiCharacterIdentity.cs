using UnityEngine;

public class KapiCharacterIdentity : MonoBehaviour
{
    [SerializeField]
    private CharacterID characterID;

    public CharacterID ID => characterID;
}