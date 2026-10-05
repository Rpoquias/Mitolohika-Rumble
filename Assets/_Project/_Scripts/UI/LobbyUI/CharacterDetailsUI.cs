using TMPro;
using UnityEngine;

public class CharacterDetailsUI : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private TMP_Text characterNameText;

    [Header("Ability")]
    [SerializeField] private TMP_Text abilityNameText;
    [SerializeField] private TMP_Text effectText;

    [Header("Stats")]
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text durationText;

    public void ShowCharacter(CharacterData character)
    {
        if (character == null)
            return;

        characterNameText.text =
            character.characterName;

        abilityNameText.text =
            character.signatureAbility;

        effectText.text =
            character.abilityDescription;

        // Range
        if (character.abilityRange > 0f)
        {
            rangeText.gameObject.SetActive(true);

            rangeText.text =
                $"RANGE  {character.abilityRange:0.#}m";
        }
        else
        {
            rangeText.gameObject.SetActive(false);
        }

        // Cooldown
        if (character.abilityCooldown > 0f)
        {
            cooldownText.gameObject.SetActive(true);

            cooldownText.text =
                $"CD  {character.abilityCooldown:0.#}s";
        }
        else
        {
            cooldownText.gameObject.SetActive(false);
        }

        // Duration
        if (character.abilityDuration > 0f)
        {
            durationText.gameObject.SetActive(true);

            durationText.text =
                $"DURATION  {character.abilityDuration:0.#}s";
        }
        else
        {
            durationText.gameObject.SetActive(false);
        }
    }
}