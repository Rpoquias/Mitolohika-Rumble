
using System;
using System.Text;
using UnityEngine;

public static class PlayerProfile
{
    public const int MaxNameLength = 16;

    private const string PlayerNameKey = "PlayerName";
    private const string DefaultPlayerName = "Player";

    public static string LocalName
    {
        get
        {
            string savedName = PlayerPrefs.GetString(
                PlayerNameKey,
                DefaultPlayerName);

            return Sanitize(savedName);
        }
    }

    public static void Save(string name)
    {
        string cleanName = Sanitize(name);

        if (string.IsNullOrEmpty(cleanName))
            cleanName = DefaultPlayerName;

        PlayerPrefs.SetString(PlayerNameKey, cleanName);
        PlayerPrefs.Save();
    }

    public static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        StringBuilder result = new StringBuilder();

        foreach (char character in name.Trim())
        {
            // Prevent TMP rich-text tags and control characters.
            if (char.IsControl(character) ||
                character == '<' ||
                character == '>')
            {
                continue;
            }

            result.Append(character);

            if (result.Length >= MaxNameLength)
                break;
        }

        return result.ToString().Trim();
    }
}
