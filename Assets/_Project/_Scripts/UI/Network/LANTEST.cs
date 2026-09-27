using UnityEngine;

public class LANTest : MonoBehaviour
{
    public void StartHost()
    {
        NetworkManager.Instance.HostLAN("LAN_TEST");
    }

    public void StartClient()
    {
        NetworkManager.Instance.JoinLAN("127.0.0.1");
    }
}