
using TMPro;
using UnityEngine;

public class IPDisplay : MonoBehaviour
{
   [SerializeField] private TMP_Text hostIPText;

   private void Start()
{
    ShowHostIP();
}

private void ShowHostIP()
{
    if (hostIPText == null)
        return;

    if (NetworkManager.Instance == null)
        return;

    string ip = NetworkManager.Instance.LANHostIP;

    if (string.IsNullOrEmpty(ip))
    {
        hostIPText.gameObject.SetActive(false);
        return;
    }

    hostIPText.text = $"Host IP: {ip}";
}
}
