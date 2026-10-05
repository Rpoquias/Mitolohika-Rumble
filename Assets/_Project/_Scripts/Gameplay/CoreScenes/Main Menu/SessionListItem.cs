using TMPro;
using UnityEngine;
using Fusion;

public class SessionListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;

    private SessionInfo session;

    public void Setup(SessionInfo sessionInfo)
    {
        session = sessionInfo;

        roomNameText.text = session.Name;
        playerCountText.text =
            $"{session.PlayerCount}/{session.MaxPlayers}";
    }

    public void Join()
    {
        NetworkManager.Instance.JoinSession(session);
    }
}