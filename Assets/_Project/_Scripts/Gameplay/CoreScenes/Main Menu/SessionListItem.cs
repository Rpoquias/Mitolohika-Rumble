
using TMPro;
using UnityEngine;
using Fusion;

public class SessionListItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;

    private SessionInfo session;
    private bool hasSession;

    public void Setup(SessionInfo sessionInfo)
    {
        session = sessionInfo;
        hasSession = true;

        if (roomNameText != null)
            roomNameText.text = session.Name;

        if (playerCountText != null)
        {
            playerCountText.text =
                $"{session.PlayerCount}/{session.MaxPlayers}";
        }
    }

    public void Join()
    {
        Debug.Log("[SESSION LIST ITEM] Join button clicked.");

        if (!hasSession)
        {
            Debug.LogWarning(
                "[SESSION LIST ITEM] No session assigned.");

            return;
        }

        NetworkManager manager = NetworkManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[SESSION LIST ITEM] NetworkManager.Instance is null.");

            return;
        }

        if (!session.IsValid ||
            !session.IsOpen ||
            session.PlayerCount >= session.MaxPlayers)
        {
            manager.ShowStatusMessage(
                "ROOM UNAVAILABLE.\nPlease refresh the session list.");

            return;
        }

        Debug.Log(
            $"[SESSION LIST ITEM] Attempting to join '{session.Name}' " +
            $"({session.PlayerCount}/{session.MaxPlayers}).");

       
        manager.JoinSession(session);
    }
}
