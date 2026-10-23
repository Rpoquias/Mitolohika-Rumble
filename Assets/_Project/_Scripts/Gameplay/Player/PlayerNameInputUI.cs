
using TMPro;
using UnityEngine;

public class PlayerNameInputUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;

    private void Awake()
    {
        if (nameInputField == null)
        {
            Debug.LogError(
                "[PLAYER NAME UI] Name input field is not assigned.",
                this);

            return;
        }

        nameInputField.characterLimit = PlayerProfile.MaxNameLength;
        nameInputField.SetTextWithoutNotify(PlayerProfile.LocalName);

        nameInputField.onEndEdit.AddListener(SaveName);
    }

    private void OnDestroy()
    {
        if (nameInputField != null)
            nameInputField.onEndEdit.RemoveListener(SaveName);
    }

    public void SaveName()
    {
        if (nameInputField != null)
            SaveName(nameInputField.text);
    }

    private void SaveName(string value)
    {
        PlayerProfile.Save(value);

        if (nameInputField != null)
        {
            nameInputField.SetTextWithoutNotify(
                PlayerProfile.LocalName);
        }

        // If MatchSession exists, request an identity update.
        // The host validates the name and preserves the player's color.
        if (MatchSession.Instance != null)
            MatchSession.Instance.RequestPlayerName(PlayerProfile.LocalName);
    }
}
