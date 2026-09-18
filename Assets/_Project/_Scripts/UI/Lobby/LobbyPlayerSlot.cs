using TMPro;
using Fusion;
using UnityEngine;

public class LobbyPlayerSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text characterText;
    [SerializeField] private TMP_Text readyText;

    private PlayerRef player;

    public void Setup(PlayerRef playerRef)
    {
        player = playerRef;

        playerNameText.text =
            $"Player {player.PlayerId + 1}";

        Refresh();
    }

    public void Refresh()
    {
        if (!player.IsValid)
            return;

        if (NetworkManager.Instance == null)
            return;

        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null)
            return;

        if (!runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            characterText.text = "Loading...";
            readyText.text = "Not Ready";
            return;
        }

        NetworkPlayerState state =
            playerObject.GetComponent<NetworkPlayerState>();

        if (state == null)
        {
            characterText.text = "None";
            readyText.text = "Not Ready";
            return;
        }

        characterText.text =
            state.SelectedCharacter.ToString();

        readyText.text =
            state.IsReady ? "Ready" : "Not Ready";
    }
}