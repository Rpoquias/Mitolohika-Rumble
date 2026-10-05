using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JoinLobbyUI : MonoBehaviour
{
    [SerializeField] private Transform sessionListParent;
    [SerializeField] private GameObject sessionItemPrefab;

    private readonly List<GameObject> spawnedItems = new();

    private void OnEnable()
    {
        NetworkManager.Instance.OnSessionListUpdatedEvent
            += RefreshSessionList;
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnSessionListUpdatedEvent
                -= RefreshSessionList;
        }
    }

    private void RefreshSessionList(List<Fusion.SessionInfo> sessions)
    {
        ClearList();

        foreach (Fusion.SessionInfo session in sessions)
        {
            if (!session.IsOpen)
                continue;

            GameObject item = Instantiate(
                sessionItemPrefab,
                sessionListParent
            );

            SessionListItem sessionItem =
                item.GetComponent<SessionListItem>();

            sessionItem.Setup(session);

            spawnedItems.Add(item);
        }
    }

    private void ClearList()
    {
        foreach (GameObject item in spawnedItems)
        {
            Destroy(item);
        }

        spawnedItems.Clear();
    }
}