
using TMPro;
using UnityEngine;

public class CharacterDetailsUI : MonoBehaviour
{
    [Header("Default Character")]
    [SerializeField]
    private CharacterData defaultCharacter;

    [Header("Character")]
    [SerializeField] private TMP_Text characterNameText;

    [Header("Ability")]
    [SerializeField] private TMP_Text abilityNameText;
    [SerializeField] private TMP_Text effectText;

    [Header("Stats")]
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text durationText;

    private CharacterData currentCharacter;

    private void Start()
    {
        // Show the default selection without requiring a button click.
        ShowCharacter(defaultCharacter);
    }

    public void ShowCharacter(CharacterData character)
    {
        if (character == null)
            return;

        currentCharacter = character;

        if (characterNameText != null)
            characterNameText.text = character.characterName;

        if (abilityNameText != null)
            abilityNameText.text = character.signatureAbility;

        if (effectText != null)
            effectText.text = character.abilityDescription;

        SetStat(
            rangeText,
            character.abilityRange,
            "RANGE",
            "m"
        );

        SetStat(
            cooldownText,
            character.abilityCooldown,
            "CD",
            "s"
        );

        SetStat(
            durationText,
            character.abilityDuration,
            "DURATION",
            "s"
        );
    }

    private static void SetStat(
        TMP_Text text,
        float value,
        string label,
        string suffix)
    {
        if (text == null)
            return;

        bool show = value > 0f;
        text.gameObject.SetActive(show);

        if (show)
            text.text = $"{label}  {value:0.#}{suffix}";
    }
}