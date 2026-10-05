using UnityEngine;

public class CharacterIdentity : MonoBehaviour
{
    [SerializeField]
    private CharacterID characterID;

    public CharacterID ID => characterID;
}